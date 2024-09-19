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

        Parallel.ForEach(Enum.GetValues<Source>(), (source, _) =>
        {
            result.Add(new(
                Source: source,
                English: BuildVocab(source, englishWordFrequencies, size),
                Japanese: BuildVocab(source, japaneseWordFrequencies, size)));
        });

        return result;
    }

    private static VocabWord[] BuildVocab(Source source, WordFrequencies wordFrequencies, int size)
    {
        List<VocabWord> words = [];
        WordFrequenciesForSource wordFreqsForSource = wordFrequencies[source];

        // For NGD, it seems like it may be better to use the maximum of any f(x), f(y), or f(xy) as the normalization
        // factor rather than the total size (M) so that the addition of new quotes has a limited effect on similarity
        // scores, only reducing the score for a word if it becomes more common in the larger set. (See page 6.)
        long normalizingFactor = Math.Max(wordFreqsForSource.SourceSize,
            wordFreqsForSource.SourceSize == 0 ? 0 :
            wordFreqsForSource.Max(x => Math.Max(
                wordFrequencies.GetTotalStemFrequency(x.Key),
                x.Value.StemFrequency)));

        foreach (var (stem, wordFreqsForStem) in wordFreqsForSource)
        {
            string word = wordFreqsForStem.GetMostCommonWordForm();
            long totalFreq = wordFrequencies.GetTotalStemFrequency(stem);
            long sourceFreq = wordFreqsForStem.StemFrequency;
            double score = CalculateSignificanceScore(
                totalSize: normalizingFactor,
                sourceSize: wordFreqsForSource.SourceSize,
                totalFreq: totalFreq,
                sourceFreq: sourceFreq);

            words.Add(new(word, sourceFreq, totalFreq, score));
        }

        return words
            .OrderByDescending(x => x.Score)
            .Take(size)
            .ToArray();
    }

    private static double CalculateSignificanceScore(long totalSize, long sourceSize, long totalFreq, long sourceFreq)
        => Math.Exp(CalculateNgd(totalFreq, sourceSize, sourceFreq, totalSize) * -1);

    /// <summary>
    /// Calculates the normalized Google distance, as described in <see href="https://arxiv.org/pdf/cs/0412098v3"/>
    /// (III.3, page 5).
    /// </summary>
    /// <param name="fx">The number of results for "x".</param>
    /// <param name="fy">The number of results for "y".</param>
    /// <param name="fxy">The number of results for "x AND y".</param>
    /// <param name="n">The total number of entries.</param>
    private static double CalculateNgd(long fx, long fy, long fxy, long n)
    {
        double logFx = Math.Log(fx);
        double logFy = Math.Log(fy);
        double logFxy = Math.Log(fxy);
        double logN = Math.Log(n);

        return (Math.Max(logFx, logFy) - logFxy) /
               (logN - Math.Min(logFx, logFy));
    }
}
