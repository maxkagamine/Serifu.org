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
using System.Text.RegularExpressions;

namespace Serifu.Data.Elasticsearch;

public sealed partial class ElasticsearchService : IElasticsearchService
{
    private const int PageSize = 39;
    private const string WeightedRandomScriptId = "weighted_random";

    // Sync with constants in search-box.ts
    private const int MaxLengthEnglish = 64;
    private const int MaxLengthJapanese = 32;

    // Used to return early if an @-mention exceeds the maximum speaker name length, also to avoid a potential attack
    // vector (a warning is thrown when generating the autocomplete json if this needs to be raised)
    private const int MaxSpeakerNameLength = 30;

    // Using a single char lets us optimize the code a bit; form feed specifically has the shortest json representation
    public const char HighlightMarker = '\f';

    private static readonly Field EnglishTextField = new("english.text");
    private static readonly Field EnglishConjugationsField = new("english.text.conjugations");
    private static readonly Field EnglishSpeakerNameField = new("english.speakerName.keyword");
    private static readonly Field JapaneseTextField = new("japanese.text");
    private static readonly Field JapaneseConjugationsField = new("japanese.text.conjugations");
    private static readonly Field JapaneseKanjiField = new("japanese.text.kanji");

    // Keep consistent with regexes in autocomplete.ts
    [GeneratedRegex(@"(?:^|\s+)[@＠](\S+)\s*(?![@＠]\S)")]
    private static partial Regex MentionRegex { get; }

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

    /// <summary>
    /// Parses and validates <paramref name="query"/> and searches Elasticsearch.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The search results, which includes the determined <see cref="SearchLanguage"/>.</returns>
    /// <exception cref="ElasticsearchValidationException">The query failed validation checks.</exception>
    /// <exception cref="ElasticsearchException">An error occurred while running the search.</exception>
    public async Task<SearchResults> Search(string query, CancellationToken cancellationToken)
    {
        try
        {
            (query, string? mention) = ExtractMention(query);
            SearchLanguage searchLanguage = DetermineSearchLanguage(query);

            if (mention is { Length: > MaxSpeakerNameLength })
            {
                return new(searchLanguage, []);
            }

            SearchRequest request = BuildSearchRequest(query, mention, searchLanguage, out Field? searchField);

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
                        searchField?.Name is string highlightFieldName &&
                        x.Highlight.TryGetValue(highlightFieldName, out var highlightedTexts) &&
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
    /// Builds the <see cref="SearchRequest"/> based on the query.
    /// </summary>
    /// <param name="query">The trimmed query without any @-mention.</param>
    /// <param name="mention">The mention (without the @ sign), or <see langword="null"/>.</param>
    /// <param name="searchLanguage">The search language, used for validation and to determine which fields to search.</param>
    /// <param name="searchField">The search field that will contain highlights.</param>
    /// <exception cref="ElasticsearchValidationException">The query is either too long or too short.</exception>
    private static SearchRequest BuildSearchRequest(
        string query,
        string? mention,
        SearchLanguage searchLanguage,
        out Field? searchField)
    {
        Query requestQuery;
        Fields? additionalFields = null;
        TermQuery? mentionQuery = null;
        searchField = null;

        if (mention is not null)
        {
            mentionQuery = new(EnglishSpeakerNameField)
            {
                Value = mention.Replace('_', ' ')
            };

            if (string.IsNullOrEmpty(query))
            {
                // Searching by speaker without a query
                return new SearchRequest()
                {
                    Query = mentionQuery,
                    Sort = [
                        SortOptions.Script(new()
                        {
                            Script = new() { Id = WeightedRandomScriptId },
                            Type = ScriptSortType.Number,
                            Order = SortOrder.Desc
                        })
                    ],
                    Size = PageSize
                };
            }
        }

        int length = new StringInfo(query).LengthInTextElements;
        int maxLength = searchLanguage is SearchLanguage.English ? MaxLengthEnglish : MaxLengthJapanese;

        if (length > maxLength)
        {
            throw new ElasticsearchValidationException(ElasticsearchValidationError.TooLong);
        }

        // To avoid a slew of irrelevant results that merely happen to have one of the same kanji or kana in it, the
        // default analyzer is configured to break Japanese into bigrams -- units of two characters -- rather than
        // unigrams (a dictionary-based tokenizer is not useful in this particular case, although one is used for
        // conjugations). However, it would still be helpful to be able to search for a single kanji, so for that reason
        // the index contains a dedicated subfield exclusively indexing kanji characters. This should also help to speed
        // up typical searches containing multiple characters.
        //
        // TODO: Consider splitting queries by whitespace so that individual kanji can be searched in conjunction with
        //  another word.
        //
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
                ],
                MinimumShouldMatch = 1
            };
        }

        if (mentionQuery is not null)
        {
            // Elasticsearch's .NET library is a bit weird: BoolQuery and MatchQuery etc. are NOT subclasses of Query as
            // you'd expect. Assignment to Query invokes an implicit conversion, and for some reason the only way to
            // access the actual query object is like this.
            if (!requestQuery.TryGet(out BoolQuery? boolQuery))
            {
                requestQuery = boolQuery = new BoolQuery() { Must = [requestQuery] };
            }

            boolQuery.Filter ??= [];
            boolQuery.Filter.Add(mentionQuery);
        }

        return new SearchRequest()
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
    }

    /// <summary>
    /// Extracts any @-mention from the query if present.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>
    /// The query without the mention (with whitespace collapsed/trimmed) and the mention (without the @ sign). If no
    /// mention is present, returns the query (still trimmed) and <see langword="null"/>.
    /// </returns>
    /// <exception cref="ElasticsearchValidationException">Multiple mentions are present in the query.</exception>
    internal static (string Query, string? Mention) ExtractMention(string query)
    {
        string? mention = null;
        query = MentionRegex.Replace(query, match =>
            {
                if (mention is not null)
                {
                    throw new ElasticsearchValidationException(ElasticsearchValidationError.MultipleMentions);
                }

                mention = match.Groups[1].Value;
                return " ";
            })
            .Trim();

        return (query, mention);
    }

    /// <summary>
    /// Detects whether <paramref name="query"/> is Japanese or not. If it is empty (i.e. searching by speaker without a
    /// query), assumes the search should be from the perspective of the user's own language.
    /// </summary>
    /// <param name="query">The trimmed query without any @-mention.</param>
    /// <returns>The determined <see cref="SearchLanguage"/>.</returns>
    internal static SearchLanguage DetermineSearchLanguage(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ?
                SearchLanguage.Japanese : SearchLanguage.English;
        }

        return UnicodeHelper.IsJapanese(query) ?
            SearchLanguage.Japanese : SearchLanguage.English;
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
