using DotNext.Collections.Generic;
using MeCab;
using MeCab.Extension.IpaDic;
using Ve.DotNet;

namespace Serifu.Analysis.WordExtractors;

internal class JapaneseWordExtractor : IWordExtractor
{
    private readonly MeCabTagger mecab = MeCabTagger.Create();

    public IEnumerable<(string Word, string Stemmed)> ExtractWords(string text)
    {
        // Similar to Serifu.ML.Tokenizers.JapaneseTokenizer
        HashSet<MeCabNode> consumedNodes = [];
        IEnumerable<MeCabNode> nodes = mecab.ParseToNodes(text);
        IEnumerable<VeWord> words = nodes.ParseVeWords();

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

            consumedNodes.AddAll(word.Tokens);

            // Using the lemma exclusively for Japanese since it's the proper dictionary form:
            // 上がれば => 上がる, 見せできます => 見せる
            yield return (word.Lemma, word.Lemma);
        }
    }
}
