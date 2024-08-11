using Kagamine.Extensions.Hosting;
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.Importer.Generic;
using Serifu.Importer.Generic.Tsv;
using Serifu.ML;
using Serilog;
using System.ComponentModel;
using System.Reflection;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

var source = Enum.Parse<Source>(args[0]);
string parserName = builder.Configuration.GetSection(args[0])[nameof(ParserOptions.Parser)] ??
    throw new Exception($"No parser configured for {source}.");

Console.Title = (typeof(Source).GetField(source.ToString())?
    .GetCustomAttribute<DescriptionAttribute>()?
    .Description ?? source.ToString()) + " Importer";

void AddParser<TParser, TOptions>()
    where TParser : class, IParser<TOptions>
    where TOptions : ParserOptions
{
    builder.Services.AddSingleton<IParser, TParser>();
    builder.Services.AddOptions<TOptions>()
        .BindConfiguration(source.ToString())
        .Configure(opts => opts.Source = source);
    builder.Services.AddTransient<IOptions<ParserOptions>>(provider =>
        provider.GetRequiredService<IOptions<TOptions>>());
}

switch (parserName)
{
    case nameof(TsvParser):
        AddParser<TsvParser, TsvParserOptions>();
        break;
    default:
        throw new Exception($"Unknown parser \"{parserName}\".");
}

builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();
builder.Services.AddSerifuMachineLearning();

builder.Services.AddScoped<GenericImporter>();

builder.Run(async (
    GenericImporter importer,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    using (logger.BeginTimedOperation("Import"))
    {
        await importer.Run(cancellationToken);
    }

    // TODO: Delete orphaned audio files
});
