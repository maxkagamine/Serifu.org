﻿using Porter2StemmerStandard;
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
            string word = RemovePossessive(text[(Range)token]);
            string stemmed = stemmer.Stem(word).Value;

            yield return (word, stemmed);
        }
    }

    private static string RemovePossessive(string word)
    {
        if (word.EndsWith("'s") || word.EndsWith("’s"))
        {
            return word[..^2];
        }

        return word;
    }
}
