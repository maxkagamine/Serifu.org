using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using Serilog;
using Xunit.Abstractions;

namespace Serifu.Data.Sqlite.Tests;

public sealed class SqliteServiceTests : IDisposable
{
    private readonly SqliteService sqliteService;
    private readonly ServiceProvider serviceProvider;
    private readonly IDbContextFactory<SerifuDbContext> dbFactory;
    private readonly Mock<HttpMessageHandler> httpHandler;

    public SqliteServiceTests(ITestOutputHelper output)
    {
        var services = new ServiceCollection();

        services
            .AddSingleton(_ =>
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                return connection;
            })
            .AddDbContextFactory<SerifuDbContext>((provider, options) => options
                .UseSqlite(provider.GetRequiredService<SqliteConnection>()));

        httpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        services.AddSingleton(httpHandler.CreateClient());

        services.AddSingleton<ILogger>(output.CreateTestLogger());

        services.AddSingleton<SqliteService>();

        serviceProvider = services.BuildServiceProvider();
        sqliteService = serviceProvider.GetRequiredService<SqliteService>();
        dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<SerifuDbContext>>();

        using var db = dbFactory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveQuotes_ReplacesQuotesForSource()
    {
        var oldTl = new Translation() { Context = "old value", SpeakerName = "old value", Text = "old value" };
        var newTl = new Translation() { Context = "new value", SpeakerName = "new value", Text = "new value" };

        var skyrimQuote = new Quote() { Id = 200, Source = Source.Skyrim, English = oldTl, Japanese = oldTl, AlignmentData = [] };

        using (var db = dbFactory.CreateDbContext())
        {
            db.Quotes.AddRange([
                new Quote() { Id = 100, Source = Source.Kancolle, English = oldTl, Japanese = oldTl, AlignmentData = [] },
                new Quote() { Id = 101, Source = Source.Kancolle, English = oldTl, Japanese = oldTl, AlignmentData = [] },
                skyrimQuote,
            ]);

            await db.SaveChangesAsync();
        }

        IEnumerable<Quote> newQuotes = [
            new Quote() { Id = 100, Source = Source.Kancolle, English = newTl, Japanese = newTl, AlignmentData = [] },
            new Quote() { Id = 102, Source = Source.Kancolle, English = newTl, Japanese = newTl, AlignmentData = [] },
        ];

        await sqliteService.SaveQuotes(Source.Kancolle, newQuotes, CancellationToken.None);

        using (var db = dbFactory.CreateDbContext())
        {
            var quotes = await db.Quotes.ToListAsync();

            quotes.Should().BeEquivalentTo([.. newQuotes, skyrimQuote]);
        }
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
