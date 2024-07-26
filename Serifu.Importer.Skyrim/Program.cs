using Kagamine.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using Serifu.Data.Sqlite;
using Serifu.Importer.Skyrim;
using Serifu.ML;
using Serilog;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuSerilog((provider, config) => config.Destructure.FormattedFormIds(provider));
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.Configure<SkyrimOptions>(builder.Configuration.GetSection("Skyrim"));

builder.Services.AddMutagen<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE, (provider, options) => options
    .WithTargetDataFolder(provider.GetRequiredService<IOptions<SkyrimOptions>>().Value.DataDirectory));

builder.Services.AddSingleton<IFormIdProvider, FormIdProvider>();

builder.Run((
    IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
    IFormIdProvider formIdProvider,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Load order:\n{LoadOrder}", formIdProvider.PrintLoadOrder());

    // TODO: Stuff
});
