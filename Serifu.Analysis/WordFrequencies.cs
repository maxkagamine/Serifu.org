using DotNext.Collections.Generic;
using Serifu.Data;
using System.Runtime.InteropServices;

namespace Serifu.Analysis;

internal class WordFrequenciesBySource : Dictionary<Source, WordFrequencies>
{
    /// <summary>
    /// Gets or creates the frequency collection for <paramref name="source"/> and adds the word and its stem,
    /// incrementing its count by one if it already exists.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="word">The word's original form.</param>
    /// <param name="stemmed">The word's stem, used for grouping conjugations.</param>
    public void Add(Source source, string word, string stemmed) => this.GetOrAdd(source).Add(word, stemmed);

    /// <summary>
    /// Gets the total count of all words for the given stem across all sources.
    /// </summary>
    /// <param name="stemmed">The stemmed word.</param>
    public int GetTotalCount(string stemmed) => this.Sum(x => x.Value.GetCount(stemmed));
}

internal class WordFrequencies
{
    // stemmed word => original form => count
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
    private readonly Dictionary<string, Dictionary<string, int>> inner = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> Stems => inner.Keys;

    /// <summary>
    /// Add the word and its stem to the frequency collection, incrementing its count by one if it already exists.
    /// </summary>
    /// <param name="word">The word's original form.</param>
    /// <param name="stemmed">The word's stem, used for grouping conjugations.</param>
    public void Add(string word, string stemmed)
    {
        var originalFormCounts = inner.GetOrAdd(stemmed, _ => new(StringComparer.OrdinalIgnoreCase));
        ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(originalFormCounts, word, out _);
        count++;
    }

    /// <summary>
    /// Gets the count of all words for the given stem.
    /// </summary>
    /// <param name="stemmed">The stemmed word.</param>
    public int GetCount(string stemmed) => inner.GetValueOrDefault(stemmed)?.Sum(x => x.Value) ?? 0;

    /// <summary>
    /// Gets the most frequent original form for the given stem.
    /// </summary>
    /// <param name="stemmed">The stemmed word.</param>
    /// <exception cref="KeyNotFoundException"/>
    public string GetMostCommonForm(string stemmed) => inner[stemmed].MaxBy(x => x.Value).Key;
}
