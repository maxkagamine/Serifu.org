namespace Serifu.ML.Abstractions;

public interface ITokenizer
{
    /// <summary>
    /// Splits text into words for alignment.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The start and end of each word as a collection of <see cref="Range"/>.</returns>
    IEnumerable<Range> Tokenize(string text);
}
