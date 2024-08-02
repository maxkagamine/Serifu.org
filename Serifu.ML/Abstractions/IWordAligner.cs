using Serifu.Data;

namespace Serifu.ML.Abstractions;

public interface IWordAligner
{
    /// <summary>
    /// Uses machine learning to map words in the English text to words in the Japanese text. The model is run in both
    /// directions and the results combined.
    /// </summary>
    /// <param name="english">The English text.</param>
    /// <param name="japanese">The Japanese text.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The resulting alignments mapping from English to Japanese.</returns>
    Task<IEnumerable<Alignment>> AlignSymmetric(string english, string japanese, CancellationToken cancellationToken = default);

    /// <summary>
    /// The tokenizer used for English.
    /// </summary>
    ITokenizer EnglishTokenizer { get; }

    /// <summary>
    /// The tokenizer used for Japanese.
    /// </summary>
    ITokenizer JapaneseTokenizer { get; }
}
