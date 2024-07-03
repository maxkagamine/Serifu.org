using Serifu.ML.Abstractions;
using System.Text.RegularExpressions;

namespace Serifu.ML.Tokenizers;

public sealed partial class EnglishTokenizer : ITokenizer
{
    [GeneratedRegex(@"\p{L}+(?:['’]\p{L}+)*|\d+(?:[,.]\d+)*")] // Adding |\S to the end would include symbols/punctuation as well
    private static partial Regex WordRegex();

    public IEnumerable<Token> Tokenize(string text)
    {
        Regex regex = WordRegex();
        List<Token> tokens = [];

        foreach (var match in regex.EnumerateMatches(text))
        {
            tokens.Add(new(match.Index, match.Index + match.Length));
        }

        return tokens;
    }
}
