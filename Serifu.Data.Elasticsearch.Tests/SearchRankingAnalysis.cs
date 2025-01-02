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
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

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

public record SourceOnlyQuote([property: JsonPropertyName("source")] Source Source);

public sealed class SearchRankingAnalysis : IClassFixture<XlsxFixture>
{
    private readonly ElasticsearchClient client;
    private readonly XLWorkbook xlsx;

    public SearchRankingAnalysis(XlsxFixture fixture)
    {
        var settings = new ElasticsearchClientSettings();
        settings.DefaultIndex(QuotesIndex.Name);
        settings.ThrowExceptions();
        client = new ElasticsearchClient(settings);
        xlsx = fixture.Xlsx;
    }

    [Theory(Explicit = true)]
    [InlineData(0, "% of top N, not weighted")]
    //
    // STOP! In order to reproduce these sheets, the elasticsearch container needs to be rebuilt with the weights
    // set as shown below. I also suggest removing the cpu & memory limits in the compose file.
    //
    // weight = 1 - (freq / total)
    //[InlineData(1, "% of top N, weighted")]
    //[InlineData(2, "% of top N, weighted^2")]
    //[InlineData(3, "% of top N, weighted^3")]
    //
    // weight = total / (freq * num_sources)
    [InlineData(1, "% of top N, alt weights")]
    [InlineData(0.5, "% of top N, alt weights^0.5")] // sqrt
    public async Task PercentOfTopNResultsBySource(double weightPower, string sheetName)
    {
        const int NumberOfQueries = 1000;
        var sheet = xlsx.Worksheet(sheetName);

        // My original hypothesis was that the proportions would become less even as the result count increased; however
        // since even 10,000 is still far away from the total count, the number of results ended up not being a factor.
        int[] topNs = [10, 20, 30, 40, 60, 80, 100, 200, 300, 400, 600, 800, 1000, 2000, 3000, 4000, 6000, 8000, 10000];

        Dictionary<Source, int[]> countsBySource = Enum.GetValues<Source>() // Source -> index in topNs -> total count
            .ToDictionary(s => s, _ => new int[topNs.Length]);

        // When size was set to the topNs[^1], I observed the proportion of each source in the returned results skewing
        // drastically in favor of whichever sources were indexed first (specifically, Kancolle quotes were appearing in
        // the results twice as often as they should have, statistically; when I reversed the array prior to indexing,
        // Kancolle dropped and BG3 quotes exploded in frequency). It seems as if ES only considers a sampling of
        // documents when doing a match_all; the only way to get accurate results is to demand every document. This
        // increases query time by an order of magnitude, however, and runs the risk of OOM. Filtering _source helped.
        await client.Indices.PutSettingsAsync(new IndexSettings()
        {
            MaxResultWindow = int.MaxValue
        }, TestContext.Current.CancellationToken);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 16,
            CancellationToken = TestContext.Current.CancellationToken
        };

        await Parallel.ForAsync(0, NumberOfQueries, parallelOptions, async (n, cancellationToken) =>
        {
            Debug.WriteLine($"Query {n} of {NumberOfQueries}");

            // Run search without a query, getting all documents in random order
            SearchResponse<SourceOnlyQuote> response = await client.SearchAsync<SourceOnlyQuote>(
                new SearchRequest()
                {
                    Query = new MatchAllQuery(),
                    Sort = [new ScriptSort()
                    {
                        Script = new Script(new InlineScript(weightPower switch
                        {
                            0 => "Math.random()",
                            1 => "Math.pow(Math.random(), 1 / doc['weight'].value)",
                            double exponent => $"Math.pow(Math.random(), 1 / Math.pow(doc['weight'].value, {exponent}))"
                        })),
                        Type = ScriptSortType.Number,
                        Order = SortOrder.Desc
                    }],
                    Size = int.MaxValue,
                    SourceIncludes = "source"
                },
                cancellationToken);

            var results = response.Documents.ToList();

            // Increment the corresponding "top Ns" for each source
            for (int i = 0; i < topNs[^1]; i++)
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
}
