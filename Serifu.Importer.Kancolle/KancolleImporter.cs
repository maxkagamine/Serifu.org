using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Models;
using Serifu.Importer.Kancolle.Services;

namespace Serifu.Importer.Kancolle;
internal class KancolleImporter : BackgroundService
{
    private readonly ShipListService shipListService;
    private readonly ShipService shipService;
    private readonly AudioFileService audioFileService;
    private readonly QuotesService quotesService;
    private readonly ILogger<KancolleImporter> logger;

    public KancolleImporter(
        ShipListService shipListService,
        ShipService shipService,
        AudioFileService audioFileService,
        QuotesService quotesService,
        ILogger<KancolleImporter> logger)
    {
        this.shipListService = shipListService;
        this.shipService = shipService;
        this.audioFileService = audioFileService;
        this.quotesService = quotesService;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();

        await quotesService.Initialize();

        var ships = await shipListService.GetShips(stoppingToken);

        foreach (Ship ship in ships)
        {
            await ImportShip(ship, stoppingToken);
        }

        sw.Stop();
        logger.LogInformation("Import completed in {Duration}.", sw.Elapsed);
        Environment.Exit(0);
    }

    private async Task ImportShip(Ship ship, CancellationToken cancellationToken = default)
    {
        var quotes = await shipService.GetQuotes(ship, cancellationToken);

        // Download each quote's audio file while checking for 404s
        foreach (Quote quote in quotes)
        {
            try
            {
                await audioFileService.DownloadAudioFile(quote, cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("{Ship}'s {Context} audio file {File} returned 404. Setting to null.",
                    quote.SpeakerEnglish, quote.Context, quote.AudioFile);

                quote.AudioFile = null;
            }
        }

        await quotesService.UpdateQuotes(ship, quotes, cancellationToken);
    }
}
