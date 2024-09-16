using Kagamine.Extensions.Collections;
using Serifu.Analysis.WordExtractors;
using Serifu.Data;
using System.Collections.Concurrent;

namespace Serifu.Analysis;

public class VocabAnalyzer
{
    private const int VocabSize = 100;

    private readonly EnglishWordExtractor englishWordExtractor = new();
    private readonly WordFrequenciesBySource englishWordFrequenciesBySource = [];

    private readonly JapaneseWordExtractor japaneseWordExtractor = new();
    private readonly WordFrequenciesBySource japaneseWordFrequenciesBySource = [];

    /// <summary>
    /// Runs a quote through the analyzer.
    /// </summary>
    public void Pipe(Quote quote)
    {
        foreach (var (word, stemmed) in englishWordExtractor.ExtractWords(quote.English.Text))
        {
            englishWordFrequenciesBySource.Add(quote.Source, word, stemmed);
        }

        foreach (var (word, stemmed) in japaneseWordExtractor.ExtractWords(quote.Japanese.Text))
        {
            japaneseWordFrequenciesBySource.Add(quote.Source, word, stemmed);
        }
    }

    /// <summary>
    /// Analyzes the collected word frequencies and builds vocab lists for each source and language.
    /// </summary>
    public IEnumerable<Vocab> BuildVocab()
    {
        ConcurrentBag<Vocab> result = [];

        Parallel.ForEach(englishWordFrequenciesBySource.Keys, (source, _) =>
        {
            result.Add(new(
                Source: source,
                English: BuildVocab(source, englishWordFrequenciesBySource),
                Japanese: BuildVocab(source, japaneseWordFrequenciesBySource)));
        });

        return result;
    }

    private static ValueArray<VocabWord> BuildVocab(Source source, WordFrequenciesBySource wordFrequenciesBySource)
    {
        List<VocabWord> words = [];

        foreach (var stemmed in wordFrequenciesBySource[source].Stems)
        {
            string word = wordFrequenciesBySource[source].GetMostCommonForm(stemmed);
            int count = wordFrequenciesBySource[source].GetCount(stemmed);
            int totalCount = wordFrequenciesBySource.GetTotalCount(stemmed);

            double score = CalculateNgd(
                totalCount,
                wordFrequenciesBySource[source].TotalCount,
                count,
                wordFrequenciesBySource.TotalCount);

            words.Add(new(word, count, totalCount, score));
        }

        // Return the N most significant words, sorted by frequency.
        return words
            .OrderByDescending(x => x.Score)
            .Take(VocabSize)
            .OrderByDescending(x => x.Count)
            .ToArray();
    }

    /// <summary>
    /// Calculated the normalized Google distance, as described in https://arxiv.org/pdf/cs/0412098v3 (III.3, page 5).
    /// </summary>
    /// <param name="x">The number of results for "x".</param>
    /// <param name="y">The number of results for "y".</param>
    /// <param name="xy">The number of results for both "x" and "y".</param>
    /// <param name="n">The total number of entries.</param>
    private static double CalculateNgd(int x, int y, int xy, int n)
    {
        double logX = Math.Log10(x);
        double logY = Math.Log10(y);
        double logXY = Math.Log10(xy);
        double logN = Math.Log10(n);

        return (Math.Max(logX, logY) - logXY) /
               (logN - Math.Min(logX, logY));
    }
}
