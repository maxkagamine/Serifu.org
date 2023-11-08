using System.Net;
using Serifu.Data.Entities;
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
    private readonly VoiceLinesService voiceLinesService;
    private readonly ILogger logger;

    public KancolleImporter(
        ShipListService shipListService,
        ShipService shipService,
        AudioFileService audioFileService,
        VoiceLinesService voiceLinesService,
        ILogger logger)
    {
        this.shipListService = shipListService;
        this.shipService = shipService;
        this.audioFileService = audioFileService;
        this.voiceLinesService = voiceLinesService;
        this.logger = logger.ForContext<KancolleImporter>();
    }

    public async Task Import(CancellationToken cancellationToken)
    {
        Console.Title = "Kancolle Importer";

        await voiceLinesService.Initialize();

        var shipsAlreadyInDb = (await voiceLinesService.GetShips(cancellationToken)).ToHashSet();
        bool skipShipsAlreadyInDb = false;
        if (shipsAlreadyInDb.Count > 0)
        {
            Console.Write("\a"); // Flashes the taskbar if the terminal's not in the foreground
            skipShipsAlreadyInDb = await new SelectionPrompt<bool>()
                .Title($"\nSkip [purple]{shipsAlreadyInDb.Count}[/] ships already in db?")
                .AddChoices(true, false)
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

                if (skipShipsAlreadyInDb && shipsAlreadyInDb.Contains(ships[i]))
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
        var voiceLines = await shipService.GetVoiceLines(ship, cancellationToken);

        // Download each voice line's audio file while checking for 404s
        foreach (VoiceLine voiceLine in voiceLines)
        {
            try
            {
                await audioFileService.DownloadAudioFile(voiceLine, cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                logger.Warning("{Ship}'s {Context} audio file {File} returned 404. Setting to null.",
                    voiceLine.SpeakerEnglish, voiceLine.Context, voiceLine.AudioFile);

                voiceLine.AudioFile = null;
            }
        }

        await voiceLinesService.UpdateVoiceLines(ship, voiceLines, cancellationToken);
    }
}
