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

using Serifu.Data;
using Serifu.Data.Local;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Services;
using Serilog;

namespace Serifu.Importer.Kancolle;
internal class KancolleImporter
{
    private readonly ShipListService shipListService;
    private readonly ShipService shipService;
    private readonly ILocalDataService localDataService;
    private readonly ILogger logger;

    public KancolleImporter(
        ShipListService shipListService,
        ShipService shipService,
        ILocalDataService localDataService,
        ILogger logger)
    {
        this.shipListService = shipListService;
        this.shipService = shipService;
        this.localDataService = localDataService;
        this.logger = logger.ForContext<KancolleImporter>();
    }

    public async Task Import(CancellationToken cancellationToken)
    {
        Console.Title = "Kancolle Importer";

        await localDataService.Initialize();

        using (logger.BeginTimedOperation(nameof(Import)))
        using (var progress = new TerminalProgressBar())
        {
            List<Quote> quotes = [];
            var ships = (await shipListService.GetShips(cancellationToken)).ToList();

            for (int i = 0; i < ships.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.SetProgress(i, ships.Count);

                var shipQuotes = await shipService.GetQuotes(ships[i], cancellationToken);

                quotes.AddRange(shipQuotes);
            }

            await localDataService.ReplaceQuotes(Source.Kancolle, quotes, cancellationToken);
        }

        await localDataService.DeleteOrphanedAudioFiles(cancellationToken);
    }
}
