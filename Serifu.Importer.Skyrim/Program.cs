using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serifu.Data.Sqlite;
using Serifu.Importer.Skyrim;
using Serifu.Importer.Skyrim.Resolvers;
using Serifu.ML;
using Serilog;

Console.Title = "Skyrim Importer";

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuSerilog((provider, config) => config.Destructure.FormattedFormIds(provider));
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.AddOptions<SkyrimOptions>().BindConfiguration("Skyrim").ValidateDataAnnotations();

builder.Services.AddMutagen<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE, (provider, options) => options
    .WithTargetDataFolder(provider.GetRequiredService<IOptions<SkyrimOptions>>().Value.DataDirectory));

builder.Services.AddSingleton<IFormIdProvider, FormIdProvider>();
builder.Services.AddSingleton<ISpeakerFactory, SpeakerFactory>();
builder.Services.AddSingleton<IFuzConverter, FuzConverter>();

builder.Services.AddSingleton<ConditionsResolver>();
builder.Services.AddSingleton<FactionResolver>();
builder.Services.AddSingleton<QuestAliasResolver>();
builder.Services.AddSingleton<SceneActorResolver>();
builder.Services.AddSingleton<UniqueVoiceTypeResolver>();
builder.Services.AddTransient(typeof(Lazy<>), typeof(LazyResolver<>));

builder.Services.AddSingleton<SkyrimImporter>();

builder.Run(async (
    SkyrimImporter importer,
    IFormIdProvider formIdProvider,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Load order:\n{LoadOrder}", formIdProvider.PrintLoadOrder());

    using (logger.BeginTimedOperation("Import"))
    {
        await importer.Run(cancellationToken);
    }
});
