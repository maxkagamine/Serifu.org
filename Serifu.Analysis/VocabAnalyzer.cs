using Serifu.Analysis.WordExtractors;
using Serifu.Data;
using System.Collections.Concurrent;

namespace Serifu.Analysis;

public class VocabAnalyzer
{
    private const int VocabSize = 100;

    private readonly WordFrequencies englishWordFrequencies = new(new EnglishWordExtractor());
    private readonly WordFrequencies japaneseWordFrequencies = new(new JapaneseWordExtractor());

    /// <summary>
    /// Runs a quote through the analyzer.
    /// </summary>
    public void Pipe(Quote quote)
    {
        englishWordFrequencies.Add(quote.Source, quote.English.Text);
        japaneseWordFrequencies.Add(quote.Source, quote.Japanese.Text);
    }

    /// <summary>
    /// Analyzes the collected word frequencies and builds vocab lists for each source and language.
    /// </summary>
    public IEnumerable<Vocab> BuildVocab(int size = VocabSize)
    {
        ConcurrentBag<Vocab> result = [];

        HashSet<string> englishStopWordStems = FindStopWordStems(englishWordFrequencies);
        HashSet<string> japaneseStopWordStems = FindStopWordStems(japaneseWordFrequencies);

        Parallel.ForEach(Enum.GetValues<Source>(), (source, _) =>
        {
            result.Add(new(
                Source: source,
                English: BuildVocab(source, englishWordFrequencies, size, englishStopWordStems),
                Japanese: BuildVocab(source, japaneseWordFrequencies, size, japaneseStopWordStems)));
        });

        return result;
    }

    private static HashSet<string> FindStopWordStems(WordFrequencies wordFrequencies)
    {
        Dictionary<string, int> sourceCountsForStems = wordFrequencies.Stems
            .ToDictionary(s => s, wordFrequencies.GetSourceCountForStem);

        int threshold = Enum.GetValues<Source>().Length / 2;

        return new(sourceCountsForStems.Where(x => x.Value > threshold).Select(x => x.Key));
    }

    private static VocabWord[] BuildVocab(Source source, WordFrequencies wordFrequencies, int size, HashSet<string> stopWordStems)
    {
        List<VocabWord> words = [];
        WordFrequenciesForSource wordFreqsForSource = wordFrequencies[source];

        foreach (var (stem, wordFreqsForStem) in wordFreqsForSource)
        {
            // TODO: Need to find a better way to identify stop words or remove this. The stem "admir" apparently
            // exists in every source; probably words like "admire" getting combined with "admiral".
            //
            // Perhaps we should limit the vocab lists to dictionary words only. Would still need to find a dictionary
            // stemmer that can de-conjugate verbs and adjectives without mangling nouns like "commander".
            //
            //if (stopWordStems.Contains(stem))
            //{
            //    continue;
            //}

            string word = wordFreqsForStem.GetMostCommonWordForm();
            long totalFreq = wordFrequencies.GetTotalStemFrequency(stem);
            long sourceFreq = wordFreqsForStem.StemFrequency;
            double score = CalculateSignificanceScore(
                totalSize: wordFrequencies.TotalSize,
                sourceSize: wordFreqsForSource.SourceSize,
                totalFreq: totalFreq,
                sourceFreq: sourceFreq);

            words.Add(new(word, sourceFreq, totalFreq, score));
        }

        return words
            //.OrderByDescending(x => x.Score)
            .OrderByDescending(x => x.Frequency)
            .Take(size)
            .ToArray();
    }


    /// <summary>
    /// Calculates the "JLH score" as used by Elasticsearch's significant terms aggregation.
    /// </summary>
    /// <remarks>
    /// “The absolute change in popularity (foregroundPercent - backgroundPercent) would favor common terms whereas
    /// the relative change in popularity (foregroundPercent / backgroundPercent) would favor rare terms. Rare vs common
    /// is essentially a precision vs recall balance and so the absolute and relative changes are multiplied to provide
    /// a sweet spot between precision and recall.”
    /// <see href="https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-bucket-significantterms-aggregation.html#_jlh_score"/>
    /// </remarks>
    private static double CalculateSignificanceScore(long totalSize, long sourceSize, long totalFreq, long sourceFreq)
    {
        double subsetProbability = (double)sourceFreq / sourceSize;
        double supersetProbability = (double)totalFreq / totalSize;

        double absoluteProbabilityChange = subsetProbability - supersetProbability;
        double relativeProbabilityChange = subsetProbability / supersetProbability;

        return absoluteProbabilityChange * relativeProbabilityChange;
    }
}
