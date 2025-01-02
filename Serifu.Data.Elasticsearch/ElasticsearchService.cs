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
                    SortOptions.Score(new ScoreSort()
                    {
                        Order = SortOrder.Desc
                    }),
                    SortOptions.Script(new ScriptSort()
                    {
                        Script = new Script(new InlineScript("Math.pow(Math.random(), 1 / doc['weight'].value)")),
                        Type = ScriptSortType.Number,
                        Order = SortOrder.Desc
                    })
                ],
                Size = PageSize
            },
            cancellationToken);

        return new SearchResults(searchLanguage, response.Hits.Select(x => new SearchResult(x.Source!)).ToArray());
    }

    private static SearchLanguage DetectLanguage(string query) =>
        Regexes.JapaneseCharacters.IsMatch(query) ? SearchLanguage.Japanese : SearchLanguage.English;
}
