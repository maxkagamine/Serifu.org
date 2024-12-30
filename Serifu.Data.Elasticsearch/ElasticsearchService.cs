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
using System.Text;

namespace Serifu.Data.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private const int PageSize = 39;

    private readonly ElasticsearchClient client;

    public ElasticsearchService(ElasticsearchClient client)
    {
        this.client = client;
    }

    public async Task<SearchResults> Search(string query, CancellationToken cancellationToken)
    {
        try
        {
            SearchLanguage searchLanguage = DetectLanguage(query);
            Field searchField = new(searchLanguage == SearchLanguage.English ? "english.text" : "japanese.text");

            SearchResponse<Quote> response = await client.SearchAsync<Quote>(
                new SearchRequest()
                {
                    Query = new BoolQuery()
                    {
                        Should = [
                            new MatchQuery(searchField)
                            {
                                Query = query,
                                MinimumShouldMatch = "75%"
                            },
                            new MatchPhraseQuery(searchField)
                            {
                                Query = query
                            }
                        ]
                    },
                    Sort = [
                        // TODO: Find a way to shuffle sources evenly to prevent those with more quotes from dominating
                        // the search results
                        SortOptions.Score(new ScoreSort()
                        {
                            Order = SortOrder.Desc
                        }),
                        SortOptions.Script(new ScriptSort()
                        {
                            Script = new Script(new InlineScript()
                            {
                                Source = "Math.random()"

                                // Below could work if we need pagination
                                //
                                //Source = "new Random(Long.parseLong(doc['id'].value) + params.seed).nextDouble()",
                                //Params = new Dictionary<string, object>()
                                //{
                                //    ["seed"] = query.GetHashCode()
                                //}
                            }),
                            Type = ScriptSortType.Number
                        })
                    ],
                    Size = PageSize
                },
                cancellationToken);

            return new SearchResults(searchLanguage, response.Hits.Select(x => new SearchResult(x.Source!)).ToArray());
        }
        catch (TransportException ex) when (ex.ApiCallDetails.HttpStatusCode is not null)
        {
            string body = Encoding.UTF8.GetString(ex.ApiCallDetails.ResponseBodyInBytes);
            throw new Exception($"Elasticsearch failed ({ex.ApiCallDetails.HttpStatusCode}): {body}", ex);
        }
    }

    private static SearchLanguage DetectLanguage(string query) =>
        Regexes.JapaneseCharacters.IsMatch(query) ? SearchLanguage.Japanese : SearchLanguage.English;
}
