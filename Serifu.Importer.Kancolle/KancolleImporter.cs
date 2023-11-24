using System.Net;
using Serifu.Data;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Models;
using Serifu.Importer.Kancolle.Services;
using Serilog;
using Spectre.Console;

namespace Serifu.Importer.Kancolle;
internal class KancolleImporter
{
    private readonly ShipListService shipListService;
    private readonly ShipService shipService;
    private readonly AudioFileService audioFileService;
    private readonly QuotesService quotesService;
    private readonly ILogger logger;

    public KancolleImporter(
        ShipListService shipListService,
        ShipService shipService,
        AudioFileService audioFileService,
        QuotesService quotesService,
        ILogger logger)
    {
        this.shipListService = shipListService;
        this.shipService = shipService;
        this.audioFileService = audioFileService;
        this.quotesService = quotesService;
        this.logger = logger.ForContext<KancolleImporter>();
    }

    public async Task Import(CancellationToken cancellationToken)
    {
        Console.Title = "Kancolle Importer";

        await quotesService.Initialize();

        var shipsAlreadyInDb = (await quotesService.GetShips(cancellationToken)).ToHashSet();
        bool skipShipsAlreadyInDb = false;
        if (shipsAlreadyInDb.Count > 0)
        {
            Console.Write("\a"); // Flashes the taskbar if the terminal's not in the foreground
            skipShipsAlreadyInDb = await new SelectionPrompt<bool>()
                .Title($"\nSkip [purple]{shipsAlreadyInDb.Count}[/] ships already in db?")
                .AddChoices(false, true)
                .UseConverter(x => x ? "Yes" : "No")
                .ShowAsync(AnsiConsole.Console, cancellationToken);

            if (skipShipsAlreadyInDb)
            {
                logger.Information("Skipping {Count} ships already in db", shipsAlreadyInDb.Count);
            }
        }

        using (logger.BeginTimedOperation(nameof(Import)))
        using (var progress = new TerminalProgressBar())
        {
            var ships = (await shipListService.GetShips(cancellationToken)).ToList();

            for (int i = 0; i < ships.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (skipShipsAlreadyInDb && shipsAlreadyInDb.Contains(ships[i].EnglishName))
                {
                    logger.Debug("{Ship} already in db", ships[i]);
                    continue;
                }

                progress.SetProgress(i, ships.Count);
                await ImportShip(ships[i], cancellationToken);
            }
        }
    }

    private async Task ImportShip(Ship ship, CancellationToken cancellationToken = default)
    {
        var quotes = await shipService.GetQuotes(ship, cancellationToken);

        // Download each quote's audio file while checking for 404s
        foreach (Quote quote in quotes)
        {
            try
            {
                await audioFileService.DownloadAudioFile(quote.Translations["ja"].AudioFile, ship, cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                logger.Warning("{Ship}'s {Context} audio file {File} returned {StatusCode}. Setting to null.",
                    quote.Translations["en"].SpeakerName,
                    quote.Translations["en"].Context,
                    quote.Translations["ja"].AudioFile?.OriginalName,
                    (int)ex.StatusCode);

                quote.Translations["ja"].AudioFile = null;
            }
        }

        await quotesService.UpdateQuotes(ship, quotes, cancellationToken);
    }
}
