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

        await sqliteService.SaveQuotes(Source.Kancolle, newQuotes);

        using (var db = dbFactory.CreateDbContext())
        {
            var quotes = await db.Quotes.ToListAsync();

            quotes.Should().BeEquivalentTo([.. newQuotes, skyrimQuote]);
        }
    }

    [Fact]
    public async Task GetCachedAudioFile_ReturnsObjectNameMatchingUri()
    {
        using (var db = dbFactory.CreateDbContext())
        {
            db.AudioFiles.AddRange([
                new() { ObjectName = "bar", Data = [] },
                new() { ObjectName = "url fragment test 1", Data = [] },
                new() { ObjectName = "url fragment test 2", Data = [] },
            ]);

            db.AudioFileCache.AddRange([
                new() { OriginalUri = new("http://example.com/foo"), ObjectName = "bar" },
                new() { OriginalUri = new("file:///Skyrim/Archive.bsa#file1"), ObjectName = "url fragment test 1" },
                new() { OriginalUri = new("file:///Skyrim/Archive.bsa#file2"), ObjectName = "url fragment test 2" }
            ]);

            await db.SaveChangesAsync();
        }

        (await sqliteService.GetCachedAudioFile(new("http://example.com/foo")))
            .Should().Be("bar");

        (await sqliteService.GetCachedAudioFile(new("file:///Skyrim/Archive.bsa#file1")))
            .Should().Be("url fragment test 1");

        (await sqliteService.GetCachedAudioFile(new("file:///Skyrim/Archive.bsa#file2")))
            .Should().Be("url fragment test 2");

        (await sqliteService.GetCachedAudioFile(new("file:///Skyrim/Archive.bsa#file3")))
            .Should().BeNull();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
