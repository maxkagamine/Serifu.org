﻿using FluentAssertions;
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

    [Fact]
    public async Task ImportAudioFile_SavesAudioFile()
    {
        byte[] bytes = [0xff, 0xfb, 0x39, 0x39, 0x39, 0x39]; // Will be recognized as an MP3
        using var stream = new MemoryStream(bytes);
        string expectedObjectName = "07/13/f029d7e1193a5ca9c63b589ea01b13d148fe.mp3";

        await sqliteService.ImportAudioFile(stream);

        using var db = dbFactory.CreateDbContext();
        AudioFile audioFile = await db.AudioFiles.SingleAsync();

        audioFile.ObjectName.Should().Be(expectedObjectName);
        audioFile.Data.ToArray().Should().Equal(bytes);
        audioFile.DateImported.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        audioFile.Size.Should().Be(bytes.Length);
        audioFile.Mode.Should().Be(0x81ff);
    }

    [Fact]
    public async Task ImportAudioFile_AddsOrUpdatesCacheEntry()
    {
        using var stream = new MemoryStream([0xff, 0xfb, 0x39, 0x39, 0x39, 0x39]);
        string expectedObjectName = "07/13/f029d7e1193a5ca9c63b589ea01b13d148fe.mp3";

        Uri originalUri1 = new("http://example.com/foo");
        Uri originalUri2 = new("http://example.com/bar");

        using (var db = dbFactory.CreateDbContext())
        {
            db.AudioFiles.Add(new() { ObjectName = "old version", Data = [] });
            db.AudioFileCache.Add(new() { OriginalUri = originalUri2, ObjectName = "old version" });

            await db.SaveChangesAsync();
        }

        await sqliteService.ImportAudioFile(stream, originalUri1);
        await sqliteService.ImportAudioFile(stream, originalUri2); // File exists, but cache entry needs to be updated

        using (var db = dbFactory.CreateDbContext())
        {
            var cache = await db.AudioFileCache.ToListAsync();

            cache.Should().BeEquivalentTo([
                new AudioFileCache() { OriginalUri = originalUri1, ObjectName = expectedObjectName },
                new AudioFileCache() { OriginalUri = originalUri2, ObjectName = expectedObjectName },
            ]);
        }
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
