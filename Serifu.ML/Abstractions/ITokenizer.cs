namespace Serifu.ML.Abstractions;

public interface ITokenizer
{
    /// <summary>
    /// Splits text into words for alignment.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The start and end of each word as a collection of <see cref="Token"/>.</returns>
    IEnumerable<Token> Tokenize(string text);

    /// <summary>
    /// Gets the number of words in <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    int GetWordCount(string text) => Tokenize(text).Count();
}
