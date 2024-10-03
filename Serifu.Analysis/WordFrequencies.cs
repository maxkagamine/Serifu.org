using DotNext.Collections.Generic;
using Serifu.Analysis.WordExtractors;
using Serifu.Data;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Serifu.Analysis;

// . -> source -> word stem -> word -> frequency used for selecting most common form/conjugation
//  `         `            `-> combined frequency used for text analysis to determine significant vocab words
//   `         `-> sum of frequencies = source (subset) size
//    `-> sum of frequencies = total (superset) size
//
// English words can take many different forms; with fictional words in the mix (or even just "-er" words like
// commander, destroyer, and witcher, which some stemmers will erroneously convert to command, destroy, and witch),
// it's hard to find a good morphological analyzer that can reliably find the "dictionary form" of a given word, so
// instead I'm using a stemmer only to group variants for frequency analysis, while simultaneously storing the most
// common forms of each word for display.
//
// Japanese on the other hand is a lot more methodological in its grammar. So far, the MeCab/Ve tokenizer's output
// seems to be sufficient, so we can feed that in as both the "stemmed word" and "original form". If needed, though,
// we can do the same thing as with English using the VeWord's Word & Lemma instead.
//
// The Add() methods are not thread-safe.
internal class WordFrequencies
{
    private readonly IWordExtractor wordExtractor;
    private readonly Dictionary<Source, WordFrequenciesForSource> sources = [];
    private readonly Dictionary<string, long> stems = new(StringComparer.OrdinalIgnoreCase);

    public WordFrequencies(IWordExtractor wordExtractor)
    {
        this.wordExtractor = wordExtractor;
    }

    public long TotalSize { get; private set; }

    public WordFrequenciesForSource this[Source source] => sources.GetValueOrDefault(source) ?? [];

    public IReadOnlyCollection<string> Stems => stems.Keys;

    public void Add(Source source, string text)
    {
        foreach (var (word, stem) in wordExtractor.ExtractWords(text))
        {
            Add(source, stem, word);
        }
    }

    private void Add(Source source, string stem, string word)
    {
        sources.GetOrAdd(source).Add(stem, word);
        ref long stemFreq = ref CollectionsMarshal.GetValueRefOrAddDefault(stems, stem, out _);
        stemFreq++;
        TotalSize++;
    }

    public long GetTotalStemFrequency(string stem) => stems[stem];

    public int GetSourceCountForStem(string stem) => sources.Count(s => s.Value.ContainsStem(stem));
}

internal class WordFrequenciesForSource : IReadOnlyDictionary<string, WordFrequenciesForStem>
{
    private readonly Dictionary<string, WordFrequenciesForStem> stems = new(StringComparer.OrdinalIgnoreCase);

    public WordFrequenciesForStem this[string stem] => stems[stem];

    public long SourceSize { get; private set; }

    public IReadOnlyCollection<string> Stems => stems.Keys;

    public void Add(string stem, string word)
    {
        stems.GetOrAdd(stem).Add(word);
        SourceSize++;
    }

    public bool ContainsStem(string stem) => stems.ContainsKey(stem);

    public IEnumerator<KeyValuePair<string, WordFrequenciesForStem>> GetEnumerator() => stems.GetEnumerator();

    #region Explicit implementation
    IEnumerable<string> IReadOnlyDictionary<string, WordFrequenciesForStem>.Keys => stems.Keys;
    IEnumerable<WordFrequenciesForStem> IReadOnlyDictionary<string, WordFrequenciesForStem>.Values => stems.Values;
    int IReadOnlyCollection<KeyValuePair<string, WordFrequenciesForStem>>.Count => stems.Count;
    bool IReadOnlyDictionary<string, WordFrequenciesForStem>.ContainsKey(string key) => stems.ContainsKey(key);
    bool IReadOnlyDictionary<string, WordFrequenciesForStem>.TryGetValue(string key, [MaybeNullWhen(false)] out WordFrequenciesForStem value) => stems.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => stems.GetEnumerator();
    #endregion
}

internal class WordFrequenciesForStem
{
    private readonly Dictionary<string, long> words = new(StringComparer.OrdinalIgnoreCase);

    public long StemFrequency { get; private set; }

    public void Add(string word)
    {
        ref long wordFreq = ref CollectionsMarshal.GetValueRefOrAddDefault(words, word, out _);
        wordFreq++;
        StemFrequency++;
    }

    public string GetMostCommonWordForm() => words.MaxBy(x => x.Value).Key.ToLowerInvariant();
}
