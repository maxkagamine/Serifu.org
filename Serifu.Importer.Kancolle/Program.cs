using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.Importer.Kancolle;
using Serifu.Importer.Kancolle.Services;
using Serifu.ML;
using Serilog;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.AddSingleton<RateLimitingHttpHandler>();
builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<RateLimitingHttpHandler>();

builder.Services.AddScoped<ShipListService>();
builder.Services.AddScoped<ShipService>();
builder.Services.AddScoped<WikiApiService>();
builder.Services.AddScoped<ContextTranslator>();

builder.Run(async (
    ShipListService shipListService,
    ShipService shipService,
    ISqliteService sqliteService,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    Console.Title = "Kancolle Importer";

    using (logger.BeginTimedOperation("Import"))
    using (var progress = new TerminalProgressBar())
    {
        List<Quote> quotes = [];
        var ships = await shipListService.GetShips(cancellationToken);

        for (int i = 0; i < ships.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.SetProgress(i, ships.Count);

            var shipQuotes = await shipService.GetQuotes(ships[i], cancellationToken);

            quotes.AddRange(shipQuotes);
        }

        await sqliteService.SaveQuotes(Source.Kancolle, quotes, cancellationToken);
    }

    await sqliteService.DeleteOrphanedAudioFiles(cancellationToken);
});
