using Serifu.Data;
using Serifu.ML.Abstractions;
using Serilog;

namespace Serifu.ML;

public class WordAligner
{
    private readonly ITransformersContext transformers;
    private readonly ILogger logger;

    public WordAligner(ITransformersContext transformers, ILogger logger)
    {
        this.transformers = transformers;
        this.logger = logger.ForContext<WordAligner>();
    }

    /// <summary>
    /// Uses machine learning to map words in the English text to words in the Japanese text. The model is run in both
    /// directions and the results combined.
    /// </summary>
    /// <param name="english">The English text.</param>
    /// <param name="japanese">The Japanese text.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The resulting alignments mapping from English to Japanese.</returns>
    public Task<IEnumerable<Alignment>> AlignSymmetric(string english, string japanese, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

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
}
