
namespace Serifu.Analysis.WordExtractors;

internal interface IWordExtractor
{
    IEnumerable<(string Word, string Stemmed)> ExtractWords(string text);
}
