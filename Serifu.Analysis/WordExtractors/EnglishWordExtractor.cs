using Porter2StemmerStandard;
using Serifu.ML.Abstractions;
using Serifu.ML.Tokenizers;

namespace Serifu.Analysis.WordExtractors;

internal class EnglishWordExtractor : IWordExtractor
{
    private readonly EnglishTokenizer tokenizer = new();
    private readonly EnglishPorter2Stemmer stemmer = new();

    public IEnumerable<(string Word, string Stemmed)> ExtractWords(string text)
    {
        foreach (Token token in tokenizer.Tokenize(text))
        {
            string word = Normalize(text[(Range)token]);

            if (!word.Any(char.IsLetter)) // Filter out numbers and such
            {
                continue;
            }

            string stemmed = stemmer.Stem(word).Value;

            yield return (word, stemmed);
        }
    }

    private static string Normalize(string word)
    {
        word = word.Replace('’', '\'');

        if (word.EndsWith("'s"))
        {
            return word[..^2];
        }

        return word;
    }
}
