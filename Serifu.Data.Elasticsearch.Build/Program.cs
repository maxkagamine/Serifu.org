using Elastic.Clients.Elasticsearch;
using Kagamine.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serifu.Data;
using Serifu.Data.Elasticsearch;
using Serifu.Data.Elasticsearch.Build;
using Serifu.Data.Sqlite;
using Serilog;
using Serilog.Events;

var builder = ConsoleApplication.CreateBuilder();

builder.Services.AddSerilog(config => config
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}"));

builder.Services.AddSerifuElasticsearch("http://localhost:9200");
builder.Services.AddSerifuSqlite("Data Source=/Serifu.db");

builder.Services.AddSingleton<ElasticsearchServer>();

builder.Run(async (
    ElasticsearchServer server,
    ElasticsearchClient elasticsearch,
    SerifuDbContext sqlite,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Starting elasticsearch");
    await server.Start(cancellationToken);

    logger.Information("Creating index");
    await elasticsearch.Indices.CreateAsync(QuotesIndex.Create(), cancellationToken);

    logger.Information("Loading quotes from sqlite db");
    List<Quote> quotes = await sqlite.Quotes.ToListAsync(cancellationToken);

    logger.Information("Indexing quotes");
    var res = await elasticsearch.BulkAsync(x => x.IndexMany(quotes), cancellationToken);
    
    if (res.Errors)
    {
        foreach (var item in res.ItemsWithErrors)
        {
            logger.Error("Failed to index quote {QuoteId}: {ErrorType}: {ErrorReason}",
                item.Id, item.Error!.Type, item.Error!.Reason);
        }

        return 1;
    }

    logger.Information("Flushing index to disk");
    await elasticsearch.Indices.RefreshAsync(cancellationToken);
    await elasticsearch.Indices.FlushAsync(cancellationToken);

    logger.Information("Stopping elasticsearch");
    await server.Stop(cancellationToken);

    logger.Information("Deleting lock files");
    var lockFiles = Directory.GetFiles("/usr/share/elasticsearch", "*.lock", SearchOption.AllDirectories);
    foreach (var file in lockFiles)
    {
        File.Delete(file);
    }

    return 0;
});

