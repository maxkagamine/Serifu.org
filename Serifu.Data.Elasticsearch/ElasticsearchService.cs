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
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Serilog;

namespace Serifu.Data.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private const int PageSize = 39;
    private const string WeightedRandomScriptId = "weighted_random";

    private readonly ElasticsearchClient client;
    private readonly ILogger logger;

    public ElasticsearchService(ElasticsearchClient client, ILogger logger)
    {
        this.client = client;
        this.logger = logger.ForContext<ElasticsearchService>();
    }

    public static PutScriptRequest WeightedRandomScript { get; } = new(WeightedRandomScriptId, "number_sort")
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
                Size = PageSize
            };

#if DEBUG
            logger.Debug("Search request:\n{Request}",
                client.RequestResponseSerializer.SerializeToString(request, SerializationFormatting.Indented));
#endif

            SearchResponse<Quote> response = await client.SearchAsync<Quote>(request, cancellationToken);

            return new SearchResults(searchLanguage, response.Hits.Select(x => new SearchResult(x.Source!)).ToArray());
        }
        catch (TransportException ex)
        {
            throw new ElasticsearchException(ex);
        }
    }

    private static SearchLanguage DetectLanguage(string query) =>
        Regexes.JapaneseCharacters.IsMatch(query) ? SearchLanguage.Japanese : SearchLanguage.English;
}
