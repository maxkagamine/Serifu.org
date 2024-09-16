using Kagamine.Extensions.Collections;
using Kagamine.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serifu.Analysis;
using Serifu.Analysis.WordExtractors;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serilog;
using Spectre.Console;
using System.ComponentModel;
using System.Reflection;

var builder = ConsoleApplication.CreateBuilder(new HostApplicationBuilderSettings()
{
    EnvironmentName = Environments.Development
});

builder.Services.AddSerifuAnalysis();
builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();

builder.Run(async (
    VocabAnalyzer vocabAnalyzer,
    ISqliteService sqliteService,
    SerifuDbContext db,
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
    async Task RunAnalysis()
    {
        IEnumerable<Vocab> vocab = [];

        // Not strictly accurate as ~250 will be removed in GetQuotesForExport, but good enough for a progress bar and
        // avoids having to load all of the quotes into memory first
        int approxCount = await db.Quotes.CountAsync(cancellationToken);

        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Analyzing", maxValue: approxCount);

            await foreach (Quote quote in sqliteService.GetQuotesForExport(cancellationToken))
            {
                vocabAnalyzer.Pipe(quote);
                task.Increment(1);
            }

            task.Value = task.MaxValue;
            vocab = vocabAnalyzer.BuildVocab();
        });

        static Table CreateTable(ValueArray<VocabWord> vocab)
        {
            var table = new Table().AddColumns("Word", "Count", "Total count", "Score");

            foreach (var word in vocab)
            {
                table.AddRow([word.Word, word.Count.ToString(), word.TotalCount.ToString(), word.Score.ToString("f4")]);
            }

            return table;
        }

        foreach (var (source, englishWords, japaneseWords) in vocab)
        {
            AnsiConsole.Write(new Rule(source.ToString()));
            AnsiConsole.Write(new Columns(CreateTable(englishWords), CreateTable(japaneseWords)));
        }
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
