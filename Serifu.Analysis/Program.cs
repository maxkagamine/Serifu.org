using Kagamine.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Serifu.Analysis.WordExtractors;
using Serifu.Data.Sqlite;
using Serilog;
using Spectre.Console;
using System.ComponentModel;
using System.Reflection;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();

builder.Run(async (
    ISqliteService sqliteService,
    ILogger logger,
    CancellationToken cancellationToken
) =>
{
    await AnsiConsole.Prompt(new SelectionPrompt<Func<Task>>()
        .Title("Program:")
        .AddChoices(RunAnalysis, TestWordExtractors)
        .UseConverter(action => action.Method.GetCustomAttribute<DisplayNameAttribute>()!.DisplayName))
        .Invoke();

    [DisplayName("Run analysis")]
    Task RunAnalysis()
    {
        throw new NotImplementedException();
    }

    [DisplayName("Test word extractors")]
    Task TestWordExtractors()
    {
        while (true)
        {
            IWordExtractor extractor = AnsiConsole.Prompt(new SelectionPrompt<(string, IWordExtractor)>()
                .Title("Language:")
                .AddChoices(
                    ("English", new EnglishWordExtractor()),
                    ("Japanese", new JapaneseWordExtractor()))
                .UseConverter(x => x.Item1))
                .Item2;

            string text = AnsiConsole.Prompt(new TextPrompt<string>("Text:"));

            var table = new Table()
                .AddColumns("Word", "Stemmed");

            foreach (var (word, stemmed) in extractor.ExtractWords(text))
            {
                table.AddRow(word, stemmed);
            }

            AnsiConsole.Write(table);
        }
    }
});
