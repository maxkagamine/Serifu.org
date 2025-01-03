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
using Serilog;
using Serilog.Events;

namespace Serifu.Data.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private const int PageSize = 39;
    private const string WeightedRandomScriptId = "weighted_random";

    // Using a single char lets us optimize the code a bit; form feed specifically has the shortest json representation
    public const char HighlightMarker = '\f';

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

    public async Task<SearchResults> Search(string query, CancellationToken cancellationToken)
    {
        try
        {
            SearchLanguage searchLanguage = DetectLanguage(query);
            string searchTranslation = searchLanguage == SearchLanguage.English ? "english" : "japanese";
            Field searchField = new($"{searchTranslation}.text");
            Field conjugationsField = new($"{searchTranslation}.text.conjugations");

            SearchRequest request = new()
            {
                Query = new BoolQuery()
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
                },
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
                            MatchedFields = Fields.FromField(conjugationsField),
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

            // TODO: Use word alignments to map highlights to opposite language

            return new SearchResults(searchLanguage, response.Hits.Select(x => new SearchResult(x.Source!)).ToArray());
        }
        catch (TransportException ex)
        {
            throw new ElasticsearchException(ex);
        }
    }

    /// <summary>
    /// Extracts the highlight ranges from <paramref name="highlightedText"/> and combines adjacent highlights.
    /// </summary>
    /// <param name="highlightedText">The highlighted text.</param>
    /// <returns>A collection of ranges indicating the start and end positions of each highlight in the original
    /// non-highlighted text.</returns>
    internal static IEnumerable<Range> ExtractHighlights(string highlightedText)
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
                if (canExtendLast && char.IsLetterOrDigit(c)) // We'll bridge whitespace gaps, hyphens, etc.
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

    private static SearchLanguage DetectLanguage(string query) =>
        Regexes.JapaneseCharacters.IsMatch(query) ? SearchLanguage.Japanese : SearchLanguage.English;
}
