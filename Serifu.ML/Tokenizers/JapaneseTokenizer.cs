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

        IEnumerable<MeCabNode> nodes = mecab.ParseToNodes(text);
        IEnumerable<VeWord> words = nodes.ParseVeWords();

        int cursor = 0;

        foreach (VeWord word in words)
        {
            if (word.PartOfSpeech == PartOfSpeech.記号) // Symbols
            {
                continue;
            }

            int start = text.IndexOf(word.Word, cursor);
            int end = start + word.Word.Length;

            tokens.Add(new(start, end));

            cursor = end;
        }

        return tokens;
    }

    public void Dispose() => mecab.Dispose();
}
