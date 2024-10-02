using Serifu.ML.Abstractions;
using System.Text.RegularExpressions;

namespace Serifu.ML.Tokenizers;

public sealed partial class EnglishTokenizer : ITokenizer
{
    [GeneratedRegex(@"\p{L}+(?:['’]\p{L}+)*|\d+(?:[,.]\d+)*")] // Adding |\S to the end would include symbols/punctuation as well
    private static partial Regex WordRegex { get; }

    public IEnumerable<Token> Tokenize(string text)
    {
        List<Token> tokens = [];

        foreach (var match in WordRegex.EnumerateMatches(text))
        {
            tokens.Add(new(match.Index, match.Index + match.Length));
        }

        return tokens;
    }
}
