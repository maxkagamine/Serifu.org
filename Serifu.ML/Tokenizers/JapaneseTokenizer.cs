using MeCab;
using MeCab.Extension.IpaDic;
using Serifu.ML.Abstractions;
using Ve.DotNet;

namespace Serifu.ML.Tokenizers;

public sealed class JapaneseTokenizer : ITokenizer, IDisposable
{
    private readonly MeCabTagger mecab = MeCabTagger.Create();

    public IEnumerable<Token> Tokenize(string text)
    {
        List<Token> tokens = [];
        HashSet<MeCabNode> consumedNodes = [];

        IEnumerable<MeCabNode> nodes = mecab.ParseToNodes(text);
        IEnumerable<VeWord> words = nodes.ParseVeWords();

        int start = 0;
        int end = 0;

        foreach (VeWord word in words)
        {
            if (word.PartOfSpeech == PartOfSpeech.記号) // Symbols
            {
                continue;
            }

            if (word.Tokens.Any(consumedNodes.Contains))
            {
                // Ve has a bug where if eatNext is true, it doesn't skip the following word on the next loop
                continue;
            }

            // Need to iterate through the tokens as VeWord.Word removes spaces which will cause IndexOf() to fail.
            // MeCabNode has a property that maybe is supposed to be the start index, but it's often zero... I'm not
            // sure if that's a bug or if I'm misunderstanding; the docs are fairly light and the names cryptic.
            bool firstToken = true;
            foreach (MeCabNode node in word.Tokens)
            {
                consumedNodes.Add(node);

                // MeCab incorrectly classifies some punctuation as nouns which messes up Ve
                if (node.Surface.All(c => !char.IsLetterOrDigit(c)))
                {
                    continue;
                }

                string surface = node.Surface.Trim();
                int nodeStart = text.IndexOf(surface, end, StringComparison.Ordinal);

                if (nodeStart == -1)
                {
                    throw new Exception($"\"{surface}\" not found in string \"{text}\" starting from {end}.");
                }

                if (firstToken)
                {
                    start = nodeStart;
                    firstToken = false;
                }

                end = nodeStart + surface.Length;
            }

            if (!firstToken)
            {
                tokens.Add(new(start, end));
            }
        }

        return tokens;
    }

    public void Dispose() => mecab.Dispose();
}
