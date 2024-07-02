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
    /// <returns>An equivalent but simplified collection of alignments.</returns>
    internal IEnumerable<Alignment> SimplifyAlignments(IEnumerable<Alignment> alignments)
    {
        throw new NotImplementedException();
    }
}
