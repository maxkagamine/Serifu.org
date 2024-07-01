using MeCab;
using MeCab.Extension.IpaDic;
using Serifu.ML.Abstractions;
using Ve.DotNet;

namespace Serifu.ML.Tokenizers;

public sealed class JapaneseTokenizer : ITokenizer, IDisposable
{
    private readonly MeCabTagger mecab = MeCabTagger.Create();

    public IEnumerable<Range> Tokenize(string text)
    {
        List<Range> ranges = [];

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

            ranges.Add(new(start, end));

            cursor = end;
        }

        return ranges;
    }

    public void Dispose() => mecab.Dispose();
}
