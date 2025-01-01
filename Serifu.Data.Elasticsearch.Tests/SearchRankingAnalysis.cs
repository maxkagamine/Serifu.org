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

using ClosedXML.Excel;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Serifu.Data.Elasticsearch.Tests;

public sealed class XlsxFixture : IDisposable
{
    private const string ResultsXlsx = @"..\..\..\SearchRankingAnalysisResults.xlsx";

    public XLWorkbook Xlsx { get; } = new(ResultsXlsx);

    public void Dispose()
    {
        Xlsx.Save();
        Xlsx.Dispose();

        // Open spreadsheet when done, since it needs to be closed while running
        Process.Start(new ProcessStartInfo(ResultsXlsx) { UseShellExecute = true });
    }
}

public sealed class SearchRankingAnalysis : IDisposable, IClassFixture<XlsxFixture>
{
    private readonly ServiceProvider serviceProvider;
    private readonly ElasticsearchClient client;
    private readonly XLWorkbook xlsx;

    public SearchRankingAnalysis(XlsxFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddSerifuElasticsearch("http://localhost:9200");

        serviceProvider = services.BuildServiceProvider();
        client = serviceProvider.GetRequiredService<ElasticsearchClient>();
        xlsx = fixture.Xlsx;
    }

    [Theory(Explicit = true)]
    [InlineData(0, "% of top N, not weighted")]
    [InlineData(1, "% of top N, weighted")]
    [InlineData(2, "% of top N, weighted^2")]
    [InlineData(3, "% of top N, weighted^3")]
    public async Task PercentOfTopNResultsBySource(int weightPower, string sheetName)
    {
        const int NumberOfQueries = 10000;
        var sheet = xlsx.Worksheet(sheetName);
        int[] topNs = [10, 20, 30, 40, 60, 80, 100, 200, 300, 400, 600, 800, 1000, 2000, 3000, 4000, 6000, 8000, 10000];

        Dictionary<Source, int[]> countsBySource = Enum.GetValues<Source>() // Source -> index in topNs -> total count
            .ToDictionary(s => s, _ => new int[topNs.Length]);

        await Parallel.ForAsync(0, NumberOfQueries, async (_, _) =>
        {
            // Run search without a query, getting all documents in random order
            SearchResponse<Quote> response = await client.SearchAsync<Quote>(
                new SearchRequest()
                {
                    Query = new MatchAllQuery(),
                    Sort = [new ScriptSort()
                    {
                        Script = new Script(new InlineScript(weightPower switch
                        {
                            0 => "Math.random()",
                            1 => "Math.pow(Math.random(), 1 / doc['weight'].value)",
                            int exponent => $"Math.pow(Math.random(), 1 / Math.pow(doc['weight'].value, {exponent}))"
                        })),
                        Type = ScriptSortType.Number,
                        Order = SortOrder.Desc
                    }],
                    Size = topNs[^1]
                },
                TestContext.Current.CancellationToken);

            var results = response.Documents.ToList();

            // Increment the corresponding "top Ns" for each source
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];

                for (int j = topNs.Length - 1; j >= 0; j--)
                {
                    if (i >= topNs[j])
                    {
                        break;
                    }

                    Interlocked.Increment(ref countsBySource[result.Source][j]);
                }
            }
        });

        // Save the results
        sheet.Range(2, 2, sheet.RowCount(), sheet.ColumnCount()).Clear();
        sheet.Cell("H1").SetValue($"Sample size = {NumberOfQueries:n0}");

        for (int i = 0; i < topNs.Length; i++)
        {
            sheet.Cell(2, 2 + i).SetValue(topNs[i]);
        }

        foreach ((int i, (Source source, int[] counts)) in countsBySource.Index())
        {
            int row = 3 + i;

            sheet.Cell(row, 1).SetValue(typeof(Source).GetField(source.ToString())?
                .GetCustomAttribute<DescriptionAttribute>()?.Description ?? source.ToString());

            for (int j = 0; j < counts.Length; j++)
            {
                sheet.Cell(row, 2 + j).SetValue(counts[j]);
            }
        }
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
