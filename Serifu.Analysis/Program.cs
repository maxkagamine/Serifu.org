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

builder.Services.AddSerifuSerilog();
builder.Services.AddSerifuSqlite();

builder.Run(async (
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
        IReadOnlyDictionary<Source, IReadOnlyList<VocabWord>> englishVocab = null!;
        IReadOnlyDictionary<Source, IReadOnlyList<VocabWord>> japaneseVocab = null!;

        // Not strictly accurate as ~250 will be removed by GetQuotesForExport, but good enough for a progress bar and
        // avoids having to load all of the quotes into memory first
        int approxCount = await db.Quotes.CountAsync(cancellationToken);
        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var task1 = ctx.AddTask("Indexing words", maxValue: approxCount);

            VocabAnalyzer englishVocabAnalyzer = new(new EnglishWordExtractor(), q => q.English);
            VocabAnalyzer japaneseVocabAnalyzer = new(new JapaneseWordExtractor(), q => q.Japanese);

            await foreach (Quote quote in sqliteService.GetQuotesForExport(cancellationToken))
            {
                englishVocabAnalyzer.Pipe(quote);
                japaneseVocabAnalyzer.Pipe(quote);

                task1.Increment(1);
            }

            task1.Value = approxCount;
            var task2 = ctx.AddTask("Analyzing");
            task2.IsIndeterminate = true;

            englishVocab = englishVocabAnalyzer.ExtractVocab();
            japaneseVocab = japaneseVocabAnalyzer.ExtractVocab();

            task2.IsIndeterminate = false;
            task2.Value = 100;
        });

        foreach (Source source in englishVocab.Keys)
        {
            static Table CreateTable(IReadOnlyList<VocabWord> vocab)
            {
                var table = new Table().AddColumns("Word", "Count", "Total count", "Score");

                foreach (var word in vocab)
                {
                    table.AddRow([word.Word, word.Count.ToString(), word.TotalCount.ToString(), word.Score.ToString("f4")]);
                }

                return table;
            }

            AnsiConsole.Write(new Rule(source.ToString()));
            AnsiConsole.Write(new Columns(CreateTable(englishVocab[source]), CreateTable(japaneseVocab[source])));
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
