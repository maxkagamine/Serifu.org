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

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Models;
using Serifu.Importer.Kancolle.Services;

namespace Serifu.Importer.Kancolle;
internal class KancolleImporter
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

    public async Task Import(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        await quotesService.Initialize();

        var ships = await shipListService.GetShips(cancellationToken);

        foreach (Ship ship in ships)
        {
            await ImportShip(ship, cancellationToken);
        }

        sw.Stop();
        logger.LogInformation("Import completed in {Duration}.", sw.Elapsed);
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
