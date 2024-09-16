using Kagamine.Extensions.Collections;

namespace Serifu.Data;

/// <summary>
/// Holds the vocab for a given source. These are the words found through text analysis to be most prevalant in the
/// source but not as common in quotes for other sources.
/// </summary>
/// <param name="Source">The source.</param>
/// <param name="English">The source's English vocab.</param>
/// <param name="Japanese">The source's Japanese vocab.</param>
public record Vocab(Source Source, ValueArray<VocabWord> English, ValueArray<VocabWord> Japanese);

/// <summary>
/// Represents a vocab word resulting from text analysis of all of the quotes in the database.
/// </summary>
/// <param name="Word">The vocab word.</param>
/// <param name="Count">The number of occurrences of the word and its conjugations in the source.</param>
/// <param name="TotalCount">The number of occurrences of the word and its conjugations across all sources.</param>
/// <param name="Score">A score indicating how closely the word aligns with this source as opposed to others.</param>
public record VocabWord(string Word, int Count, int TotalCount, double Score);
