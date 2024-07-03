// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using DotNext.Threading;
using Kagamine.Extensions.Logging;
using Serifu.Data;
using Serifu.ML.Abstractions;
using Serifu.ML.Tokenizers;
using Serilog;

namespace Serifu.ML;

public sealed class WordAligner : IWordAligner, IDisposable
{
    private const string Model = "qiyuw/WSPAlign-ft-kftt";
    private const string Marker = " ¶ ";
    private const double Threshold = 0.1;
    private const int BatchSize = 32;

    private readonly ITransformersContext transformers;
    private readonly ILogger logger;

    private readonly EnglishTokenizer englishTokenizer = new();
    private readonly JapaneseTokenizer japaneseTokenizer = new();

    private readonly AsyncLazy<IQuestionAnsweringPipeline> pipeline;

    public WordAligner(ITransformersContext transformers, ILogger logger)
    {
        this.transformers = transformers;
        this.logger = logger
            .ForContext<WordAligner>()
            .ForContext(nameof(ITransformersContext.DeviceName), transformers.DeviceName)
            .ForContext(nameof(BatchSize), BatchSize);

        pipeline = new(cancellationToken =>
        {
            this.logger.Information("Loading model");
            return this.transformers.QuestionAnswering(Model, BatchSize, cancellationToken);
        });
    }

    public async Task<IEnumerable<Alignment>> AlignSymmetric(string english, string japanese, CancellationToken cancellationToken = default)
    {
        // Load the model before starting the timed operation, as it may have to download
        var pipeline = await this.pipeline.WithCancellation(cancellationToken);

        // These tokenizers are slightly different from what WSPAlign was trained with: the English tokenizer doesn't
        // split contractions, and the Japanese tokenizer use Ve to recombine MeCab tokens into words, as the latter
        // tends to tokenize into morphemes which aren't really natural or useful for word alignment (e.g. しました ->
        // し, まし, た). It might be best to retrain the model, but it seems to be fine as is.
        Token[] englishTokens = englishTokenizer.Tokenize(english).ToArray();
        Token[] japaneseTokens = japaneseTokenizer.Tokenize(japanese).ToArray();

        using (logger.ForContext("WordCount", englishTokens.Length + japaneseTokens.Length) // For statistics
                     .BeginTimedOperation("Running word alignment"))
        {
            var forwardAlignments = await AlignForward(pipeline, english, japanese, englishTokens, japaneseTokens, cancellationToken);
            var reverseAlignments = await AlignReverse(pipeline, english, japanese, englishTokens, japaneseTokens, cancellationToken);

            return SimplifyAlignments(forwardAlignments.Concat(reverseAlignments), english, japanese);
        }
    }

    /// <summary>
    /// Runs the ML model to find the tokens in the <paramref name="toText"/> that align with each of the tokens in the
    /// <paramref name="fromText"/>.
    /// </summary>
    /// <param name="pipeline">The transformers pipeline.</param>
    /// <param name="fromText">The "from" text.</param>
    /// <param name="toText">The "to" text.</param>
    /// <param name="fromTokens">The tokens for the "from" text, to mark words for alignment.</param>
    /// <param name="toTokens">The tokens for the "to" text, to align predictions to whole words.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Unsimplified alignments.</returns>
    private static async Task<IEnumerable<Alignment>> AlignForward(
        IQuestionAnsweringPipeline pipeline,
        string fromText,
        string toText,
        Token[] fromTokens,
        Token[] toTokens,
        CancellationToken cancellationToken)
    {
        string[] questions = fromTokens.Select(t => WrapToken(fromText, t)).ToArray();
        QuestionAnsweringPrediction[] predictions = await pipeline.Pipe(questions, toText, cancellationToken);

        List<Alignment> alignments = new(predictions.Length);

        for (int i = 0; i < predictions.Length; i++)
        {
            var (fromStart, fromEnd) = fromTokens[i];
            QuestionAnsweringPrediction prediction = predictions[i];

            if (prediction.Score < Threshold)
            {
                continue;
            }

            foreach (var (toStart, toEnd) in GetOverlappingTokens(prediction, toTokens))
            {
                alignments.Add(new(fromStart, fromEnd, toStart, toEnd));
            }
        }

        return alignments;
    }

    /// <summary>
    /// Calls <see cref="AlignForward(IQuestionAnsweringPipeline, string, string, Token[], Token[],
    /// CancellationToken)"/> with the from and to swapped, then swaps the results back.
    /// </summary>
    /// <param name="pipeline">The transformers pipeline.</param>
    /// <param name="fromText">The "from" text.</param>
    /// <param name="toText">The "to" text.</param>
    /// <param name="fromTokens">The tokens for the "from" text, to align predictions to whole words.</param>
    /// <param name="toTokens">The tokens for the "to" text, to mark words for alignment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Unsimplified alignments.</returns>
    private static async Task<IEnumerable<Alignment>> AlignReverse(
        IQuestionAnsweringPipeline pipeline,
        string fromText,
        string toText,
        Token[] fromTokens,
        Token[] toTokens,
        CancellationToken cancellationToken)
    {
        var alignments = await AlignForward(pipeline, toText, fromText, toTokens, fromTokens, cancellationToken);
        return alignments.Select(a => new Alignment(a.ToStart, a.ToEnd, a.FromStart, a.FromEnd));
    }

    /// <summary>
    /// Wraps the part of the <paramref name="text"/> to be aligned.
    /// </summary>
    /// <param name="text">The "from" text.</param>
    /// <param name="tokenRange">The range of the "from" text to mark for alignment.</param>
    private static string WrapToken(string text, Range tokenRange)
        => $"{text[..tokenRange.Start]}{Marker}{text[tokenRange]}{Marker}{text[tokenRange.End..]}";

    /// <summary>
    /// Finds the tokens that intersect the prediction span.
    /// </summary>
    /// <param name="prediction">The prediction.</param>
    /// <param name="tokens">The "to" tokens.</param>
    private static IEnumerable<Token> GetOverlappingTokens(QuestionAnsweringPrediction prediction, Token[] tokens)
        => tokens.Where(t => prediction.End > t.Start && prediction.Start < t.End);

    /// <summary>
    /// An algorithm for merging word alignments, such that for any selected span of the "from" text, the resulting
    /// aligned span(s) in the "to" text remains the same, and vice versa.
    /// </summary>
    /// <remarks>
    /// Two alignments are eligible to be merged if:
    /// <list type="bullet">
    ///   <item>One is entirely contained within the other on both sides; or</item>
    ///   <item>Both point to the same span on one side and are overlapping, adjacent, or separated by only whitespace
    ///   on the other.</item>
    /// </list>
    /// Ported from <see href="https://github.com/maxkagamine/word-alignment-demo/blob/master/simplify.py"/>.<br />
    /// This isn't guaranteed to produce the most optimal result in all cases, but it's fast and good enough for
    /// real-world sentence pairs. See simplify.py and simplify_slow.py for more details.
    /// </remarks>
    /// <param name="alignments">A collection of alignments.</param>
    /// <param name="fromText">The "from" text.</param>
    /// <param name="toText">The "to" text.</param>
    /// <returns>An equivalent but simplified collection of alignments.</returns>
    internal static IEnumerable<Alignment> SimplifyAlignments(IEnumerable<Alignment> alignments, string fromText, string toText)
    {
        // Remove duplicates from symmetrizing and sort. Because the result will only shrink, we can use a span and sort
        // in-place later while shrinking our view of the array to avoid allocating new arrays with each iteration.
        Span<Alignment> result = alignments.Distinct().Order().ToArray();

        // Make repeated forward-passes through the array until there's nothing left to merge
        while (true)
        {
            int removedCount = 0;

            for (int i = 0; i < result.Length - 1; i++)
            {
                ref Alignment current = ref result[i];
                if (current == default)
                {
                    continue;
                }

                for (int j = i + 1; j < result.Length; j++)
                {
                    ref Alignment other = ref result[j];
                    if (other == default)
                    {
                        continue;
                    }

                    if (TryMergeAlignments(current, other, fromText, toText, out Alignment merged))
                    {
                        current = merged;
                        other = default;
                        removedCount++;
                    }
                }
            }

            if (removedCount == 0)
            {
                break;
            }

            // Sort the span and trim off the ones we removed, which will be at the top. This is faster than Order(), as
            // we don't need to create any new arrays.
            MemoryExtensions.Sort(result);
            result = result[removedCount..];
        }

        return result.ToArray();
    }

    /// <summary>
    /// Tries to merge two alignments. See the remarks of <see cref="SimplifyAlignments(IEnumerable{Alignment}, string,
    /// string)"/> for details.
    /// </summary>
    /// <param name="left">The first alignment.</param>
    /// <param name="right">The second alignment.</param>
    /// <param name="fromText">The "from" text.</param>
    /// <param name="toText">The "to" text.</param>
    /// <param name="merged">The merged alignment, or <see langword="default"/> if the two alignments cannot be
    /// merged.</param>
    /// <returns>A boolean indicating whether the alignments were eligible to be merged.</returns>
    private static bool TryMergeAlignments(Alignment left, Alignment right, string fromText, string toText, out Alignment merged)
    {
        // Check if left is entirely contained within right
        if (left.FromStart >= right.FromStart && left.FromEnd <= right.FromEnd &&
            left.ToStart >= right.ToStart && left.ToEnd <= right.ToEnd)
        {
            merged = right;
            return true;
        }

        // Check if right is entirely contained within left
        if (right.FromStart >= left.FromStart && right.FromEnd <= left.FromEnd &&
            right.ToStart >= left.ToStart && right.ToEnd <= left.ToEnd)
        {
            merged = left;
            return true;
        }

        // Check if "from" is the same and "to" is overlapping/adjacent
        if (left.FromStart == right.FromStart && left.FromEnd == right.FromEnd &&
            IsOverlappingOrAdjacent(left.ToStart, left.ToEnd, right.ToStart, right.ToEnd, toText))
        {
            merged = new(left.FromStart, left.FromEnd, Math.Min(left.ToStart, right.ToStart), Math.Max(left.ToEnd, right.ToEnd));
            return true;
        }

        // Check if "to" is the same and "from" is overlapping/adjacent
        if (left.ToStart == right.ToStart && left.ToEnd == right.ToEnd &&
            IsOverlappingOrAdjacent(left.FromStart, left.FromEnd, right.FromStart, right.FromEnd, fromText))
        {
            merged = new(Math.Min(left.FromStart, right.FromStart), Math.Max(left.FromEnd, right.FromEnd), left.ToStart, left.ToEnd);
            return true;
        }

        merged = default;
        return false;
    }

    /// <summary>
    /// Returns true if [start1,end1) overlaps with, touches, or is separated by only whitespace from [start2,end2).
    /// </summary>
    /// <param name="start1">The inclusive start index of the first range.</param>
    /// <param name="end1">The exclusive end index of the first range.</param>
    /// <param name="start2">The inclusive start index of the second range.</param>
    /// <param name="end2">The exclusive end index of the second range.</param>
    /// <param name="text">The text to which these ranges apply.</param>
    private static bool IsOverlappingOrAdjacent(ushort start1, ushort end1, ushort start2, ushort end2, string text)
    {
        return (end1 < start2 && string.IsNullOrWhiteSpace(text[end1..start2])) ||
               (start1 <= end2 && end1 >= start2) ||
               (start1 > end2 && string.IsNullOrWhiteSpace(text[end2..start1]));
    }

    public void Dispose()
    {
        japaneseTokenizer.Dispose();
    }
}
