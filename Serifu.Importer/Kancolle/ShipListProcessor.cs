using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serifu.Data;
using Serifu.Data.Entities;
using Serifu.Importer.Helpers;

namespace Serifu.Importer.Kancolle;

/// <summary>
/// Fetches the list of ships from the wiki and places them on the queue to be processed by <see cref="ShipProcessor"/>.
/// All audio files for Kancolle quotes already in the database are also added to the audio file queue so that the <see
/// cref="AudioFileProcessor"/> can immediately pick up where it left off.
/// </summary>
internal class ShipListProcessor : IProcessor
{
    const string WikiShipListUrl = "https://en.kancollewiki.net/Ship_list";

    private readonly Channel<ShipQueueItem> shipQueue;
    private readonly Channel<AudioFileQueueItem> audioFileQueue;
    private readonly QuotesContext db;
    private readonly HttpClient httpClient;
    private readonly ILogger<ShipListProcessor> logger;

    public ShipListProcessor(
        Channel<ShipQueueItem> shipQueue,
        Channel<AudioFileQueueItem> audioFileQueue,
        QuotesContext db,
        HttpClient httpClient,
        ILogger<ShipListProcessor> logger)
    {
        this.shipQueue = shipQueue;
        this.audioFileQueue = audioFileQueue;
        this.db = db;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting ship list");
        var html = await httpClient.GetStringAsync(WikiShipListUrl, cancellationToken);
        using var document = await new HtmlParser().ParseDocumentAsync(html, cancellationToken);

        var ships = document.QuerySelectorAll<IHtmlAnchorElement>("span[id^=\"shiplistkai-\"] a:not(.mw-redirect)")
            .Select(link =>
            {
                var englishName = link.TextContent.Trim();
                var japaneseName = link.ParentElement?.GetTextNodes() ?? englishName;

                return new ShipQueueItem(englishName, japaneseName);
            })
            .ToList();

        if (ships.Count == 0)
        {
            throw new Exception("Found zero ships. This probably means the CSS selector is broken.");
        }

        if (!ships.Any(s => s.JapaneseName != s.EnglishName)) // Japanese name is actually "name shown in game" which might be English or Cyrillic
        {
            throw new Exception("No ship has a Japanese name. This probably means the table structure has changed.");
        }

        // Download any missing audio files
        var quotesAlreadyInDb = await db.Quotes
            .Where(q => q.Source == Source.Kancolle && q.AudioFile != null)
            .ToListAsync(cancellationToken);
        await audioFileQueue.Writer.WriteRangeAsync(
            quotesAlreadyInDb.Select(q => new AudioFileQueueItem(q)), CancellationToken.None);

        // Pass off the list of ships to the ship worker
        await shipQueue.Writer.WriteRangeAsync(ships, CancellationToken.None);

        // Tell the ship worker it can complete once the queue is empty
        shipQueue.Writer.Complete();
    }
}
