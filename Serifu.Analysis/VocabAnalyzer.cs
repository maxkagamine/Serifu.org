using Serifu.Analysis.WordExtractors;
using Serifu.Data;

namespace Serifu.Analysis;

internal record VocabWord(string Word, int Count, int TotalCount, double Score);

internal class VocabAnalyzer
{
    private readonly IWordExtractor wordExtractor;
    private readonly Func<Quote, Translation> translationSelector;
    private readonly WordFrequenciesBySource wordFrequenciesBySource = [];

    public VocabAnalyzer(IWordExtractor wordExtractor, Func<Quote, Translation> translationSelector)
    {
        this.wordExtractor = wordExtractor;
        this.translationSelector = translationSelector;
    }

    public void Pipe(Quote quote)
    {
        string text = translationSelector(quote).Text;

        foreach (var (word, stemmed) in wordExtractor.ExtractWords(text))
        {
            wordFrequenciesBySource.Add(quote.Source, word, stemmed);
        }
    }

    public IReadOnlyDictionary<Source, IReadOnlyList<VocabWord>> ExtractVocab()
    {
        return new Dictionary<Source, IReadOnlyList<VocabWord>>();
    }
}
