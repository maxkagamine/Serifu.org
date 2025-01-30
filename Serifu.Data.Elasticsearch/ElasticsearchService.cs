// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Kagamine.Extensions.Collections;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Serifu.Data.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private const int PageSize = 39;
    private const string WeightedRandomScriptId = "weighted_random";

    // Sync with constants in search-box.ts
    private const int MaxLengthEnglish = 64;
    private const int MaxLengthJapanese = 32;

    // Using a single char lets us optimize the code a bit; form feed specifically has the shortest json representation
    public const char HighlightMarker = '\f';

    private static readonly Field EnglishTextField = new("english.text");
    private static readonly Field EnglishConjugationsField = new("english.text.conjugations");
    private static readonly Field JapaneseTextField = new("japanese.text");
    private static readonly Field JapaneseConjugationsField = new("japanese.text.conjugations");
    private static readonly Field JapaneseKanjiField = new("japanese.text.kanji");

    private readonly ElasticsearchClient client;
    private readonly ILogger logger;

    public ElasticsearchService(ElasticsearchClient client, ILogger logger)
    {
        this.client = client;
        this.logger = logger.ForContext<ElasticsearchService>();
    }

    public static PutScriptRequest WeightedRandomScript => new(WeightedRandomScriptId, "number_sort")
    {
        Script = new()
        {
            Language = ScriptLanguage.Painless,
            Source = "Math.pow(Math.random(), 1 / doc['weight'].value)"
        }
    };

    // TODO: Add tags so we can look up the translation for a quote from a specific character (e.g. "top number #Hachiroku")
    public async Task<SearchResults> Search(string query, CancellationToken cancellationToken)
    {
        try
        {
            SearchLanguage searchLanguage = UnicodeHelper.IsJapanese(query) ?
                SearchLanguage.Japanese : SearchLanguage.English;

            Field searchField;
            Fields? additionalFields = null;
            Query requestQuery;

            query = query.Trim();
            int length = new StringInfo(query).LengthInTextElements;
            int maxLength = searchLanguage is SearchLanguage.English ? MaxLengthEnglish : MaxLengthJapanese;

            if (length > maxLength)
            {
                throw new ElasticsearchValidationException(ElasticsearchValidationError.TooLong);
            }

            // To avoid a slew of irrelevant results that merely happen to have one of the same kanji or kana in it, the
            // default analyzer is configured to break Japanese into bigrams -- units of two characters -- rather than
            // unigrams (a dictionary-based tokenizer is not useful in this particular case, although one is used for
            // conjugations). However, it would still be helpful to be able to search for a single kanji, so for that
            // reason the index contains a dedicated subfield exclusively indexing kanji characters. This should also
            // help to speed up typical searches containing multiple characters.
            if (searchLanguage is SearchLanguage.Japanese && UnicodeHelper.IsSingleKanji(query))
            {
                searchField = JapaneseKanjiField;
                requestQuery = new MatchQuery(JapaneseKanjiField) { Query = query };
            }
            else if (length < 2)
            {
                throw new ElasticsearchValidationException(ElasticsearchValidationError.TooShort);
            }
            else
            {
                (searchField, Field conjugationsField) = searchLanguage is SearchLanguage.English ?
                    (EnglishTextField, EnglishConjugationsField) :
                    (JapaneseTextField, JapaneseConjugationsField);

                additionalFields = Fields.FromField(conjugationsField);

                requestQuery = new BoolQuery()
                {
                    Should = [
                        new MatchPhraseQuery(searchField)
                        {
                            Query = query
                        },
                        new MatchQuery(searchField)
                        {
                            Query = query,
                            MinimumShouldMatch = "75%"
                        },
                        new MatchQuery(conjugationsField)
                        {
                            Query = query,
                            MinimumShouldMatch = "75%"
                        }
                    ]
                };
            }

            SearchRequest request = new()
            {
                Query = requestQuery,
                Sort = [
                    SortOptions.Score(new()
                    {
                        Order = SortOrder.Desc
                    }),
                    SortOptions.Script(new()
                    {
                        Script = new() { Id = WeightedRandomScriptId },
                        Type = ScriptSortType.Number,
                        Order = SortOrder.Desc
                    })
                ],
                Highlight = new()
                {
                    Fields = new Dictionary<Field, HighlightField>()
                    {
                        [searchField] = new()
                        {
                            MatchedFields = additionalFields,
                            NumberOfFragments = 0,
                            PreTags = [HighlightMarker.ToString()],
                            PostTags = [HighlightMarker.ToString()]
                        }
                    }
                },
                Size = PageSize
            };

            if (logger.IsEnabled(LogEventLevel.Debug))
            {
                logger.Debug("Search request:\n{Request}",
                    client.RequestResponseSerializer.SerializeToString(request, SerializationFormatting.Indented));
            }

            SearchResponse<Quote> response = await client.SearchAsync<Quote>(request, cancellationToken);

            var results = response.Hits
                .Select(x =>
                {
                    Quote quote = x.Source!;

                    IReadOnlyList<Range> englishHighlights = [];
                    IReadOnlyList<Range> japaneseHighlights = [];

                    if (x.Highlight is not null &&
                        x.Highlight.TryGetValue(searchField.Name!, out var highlightedTexts) &&
                        highlightedTexts.Count == 1)
                    {
                        if (searchLanguage == SearchLanguage.English)
                        {
                            englishHighlights = ExtractHighlights(highlightedTexts.Single());
                            japaneseHighlights = MapHighlightsToTargetLanguage(englishHighlights, searchLanguage, quote.AlignmentData, quote.Japanese.Text);
                        }
                        else
                        {
                            japaneseHighlights = ExtractHighlights(highlightedTexts.Single());
                            englishHighlights = MapHighlightsToTargetLanguage(japaneseHighlights, searchLanguage, quote.AlignmentData, quote.English.Text);
                        }
                    }

                    return new SearchResult()
                    {
                        Quote = quote,
                        EnglishHighlights = englishHighlights,
                        JapaneseHighlights = japaneseHighlights
                    };
                })
                .ToArray();

            return new SearchResults(searchLanguage, results);
        }
        catch (TransportException ex)
        {
            if (ex.InnerException is TaskCanceledException)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            throw new ElasticsearchException(ex);
        }
    }

    /// <summary>
    /// Extracts the highlight ranges from <paramref name="highlightedText"/> and combines adjacent highlights.
    /// </summary>
    /// <param name="highlightedText">The highlighted text.</param>
    /// <returns>A collection of ranges indicating the start and end positions of each highlight in the original
    /// non-highlighted text, without overlaps.</returns>
    internal static IReadOnlyList<Range> ExtractHighlights(string highlightedText)
    {
        List<Range> ranges = [];
        int startIndex = 0;
        int index = 0;
        bool inHighlight = false;
        bool canExtendLast = false;

        foreach (char c in highlightedText)
        {
            if (c == HighlightMarker)
            {
                if (inHighlight) // End highlight marker
                {
                    ranges.Add(new(startIndex, index));
                    inHighlight = false;
                    canExtendLast = true;
                }
                else // Start highlight marker
                {
                    if (canExtendLast) // ...following a gap we can bridge (keep previous start index)
                    {
                        ranges.RemoveAt(ranges.Count - 1);
                    }
                    else
                    {
                        startIndex = index;
                    }

                    inHighlight = true;
                }
            }
            else
            {
                if (canExtendLast && !IsCharBridgeable(c)) // Bridge whitespace and hyphens
                {
                    canExtendLast = false;
                }

                // Index will always be that of the next "real" character to be read, meaning when a start marker is
                // encountered it's the inclusive start index, and at an end marker it's the exclusive end index.
                index++;
            }
        }

        return ranges;
    }

    /// <summary>
    /// Uses the quote's word alignments to map the highlight ranges on the search language side to the ranges of
    /// their corresponding translation on the target language side.
    /// </summary>
    /// <param name="searchHighlights">The ranges highlighted on the search language side.</param>
    /// <param name="searchLanguage">The search language.</param>
    /// <param name="alignments">The word alignments.</param>
    /// <param name="targetText">The <see cref="Translation.Text"/> opposite of the search language.</param>
    /// <returns>A collection of ranges indicating the start and end positions for highlights in the <paramref
    /// name="targetText"/>, without overlaps.</returns>
    internal static IReadOnlyList<Range> MapHighlightsToTargetLanguage(
        IReadOnlyList<Range> searchHighlights,
        SearchLanguage searchLanguage,
        ValueArray<Alignment> alignments,
        string targetText)
    {
        // Find alignments whose search-language-side intersects with the search highlights and convert them to ranges
        // on the target language side, sorted by start index
        Range[] sortedRanges = alignments
            .Where(alignment =>
            {
                // Alignments are in the direction of English -> Japanese
                var (start, end) = searchLanguage == SearchLanguage.English ?
                    (alignment.FromStart, alignment.FromEnd) : (alignment.ToStart, alignment.ToEnd);

                return searchHighlights.Any(h => h.Start.Value < end && h.End.Value > start);
            })
            .Select(a => searchLanguage == SearchLanguage.English ?
                new Range(a.ToStart, a.ToEnd) : new Range(a.FromStart, a.FromEnd))
            .OrderBy(r => r.Start.Value)
            .ToArray();

        // Bail out if no matches
        if (sortedRanges.Length == 0)
        {
            return [];
        }

        // Merge overlapping and adjacent ranges, similar to the search highlights and what we do in WordAligner
        List<Range> result = [sortedRanges[0]];

        for (int i = 1; i < sortedRanges.Length; i++)
        {
            Range prev = result[^1];
            Range current = sortedRanges[i];

            if (current.End.Value <= prev.End.Value)
            {
                // Current range is contained entirely within the previous and is redundant
                continue;
            }

            if (current.Start.Value <= prev.End.Value ||
                targetText[prev.End..current.Start].All(IsCharBridgeable))
            {
                // Current range extends the previous range
                result[^1] = new Range(prev.Start, current.End);
            }
            else
            {
                // Current range comes after the previous, and the gap is not bridgeable
                result.Add(current);
            }
        }

        return result;
    }

    /// <summary>
    /// A gap between highlights can be bridged if it consists only of characters for which this method returns true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCharBridgeable(char c) => char.IsWhiteSpace(c) || c is '-';
}
