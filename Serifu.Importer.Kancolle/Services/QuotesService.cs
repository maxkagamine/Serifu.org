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
/// Manages the quotes database.
/// </summary>
internal class QuotesService
{
    private readonly QuotesContext db;
    private readonly ILogger logger;

    public QuotesService(
        QuotesContext db,
        ILogger logger)
    {
        this.db = db;
        this.logger = logger.ForContext<QuotesService>();
    }

    public Task Initialize()
    {
        logger.Information("Database is {Path}", Path.GetFullPath(db.Database.GetDbConnection().DataSource));
        return db.Database.MigrateAsync();
    }

    /// <summary>
    /// Deletes all of the quotes for <paramref name="ship"/> and adds <paramref name="quotes"/> in their place.
    /// </summary>
    /// <param name="ship">The ship whose quotes to replace.</param>
    /// <param name="quotes">The new quotes.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task UpdateQuotes(Ship ship, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        logger.Information("Saving {Count} quotes for {Ship}", quotes.Count(), ship);

        // Remove the ship's existing quotes, as we don't have any way to identify changes to individual rows. Note that
        // this assumes no two ships will have the same English name.
        db.Quotes.RemoveRange(await db.Quotes
            .Where(q => q.Source == Source.Kancolle && q.SpeakerEnglish == ship.EnglishName)
            .ToListAsync(cancellationToken));

        // Add the new quotes.
        db.Quotes.AddRange(quotes);

        await db.SaveChangesAsync(cancellationToken);
    }
}
