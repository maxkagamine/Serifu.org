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

using Microsoft.EntityFrameworkCore;
using Serifu.Data;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Models;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Manages the voice lines in the database.
/// </summary>
internal class VoiceLinesService
{
    private readonly SerifuContext db;
    private readonly ILogger logger;

    public VoiceLinesService(
        SerifuContext db,
        ILogger logger)
    {
        this.db = db;
        this.logger = logger.ForContext<VoiceLinesService>();
    }

    public Task Initialize()
    {
        logger.Information("Database is {Path}", Path.GetFullPath(db.Database.GetDbConnection().DataSource));
        return db.Database.MigrateAsync();
    }

    /// <summary>
    /// Deletes all of the voice lines for <paramref name="ship"/> and adds <paramref name="voiceLines"/> in their place.
    /// </summary>
    /// <param name="ship">The ship whose voice lines to replace.</param>
    /// <param name="voiceLines">The new voice lines.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task UpdateVoiceLines(Ship ship, IEnumerable<VoiceLine> voiceLines, CancellationToken cancellationToken = default)
    {
        logger.Information("Saving {Count} voice lines for {Ship}", voiceLines.Count(), ship);

        // Remove the ship's existing voice lines, as we don't have any way to identify changes to individual rows. Note
        // that this assumes no two ships will have the same English name.
        db.VoiceLines.RemoveRange(await db.VoiceLines
            .Where(v => v.Source == Source.Kancolle && v.SpeakerEnglish == ship.EnglishName)
            .ToListAsync(cancellationToken));

        // Add the new voice lines.
        db.VoiceLines.AddRange(voiceLines);

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the ships already in the database.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task<IEnumerable<Ship>> GetShips(CancellationToken cancellationToken = default)
    {
        return await db.VoiceLines
            .Where(v => v.Source == Source.Kancolle)
            .Select(v => new Ship(v.SpeakerEnglish, v.SpeakerJapanese))
            .ToListAsync(cancellationToken);
    }
}
