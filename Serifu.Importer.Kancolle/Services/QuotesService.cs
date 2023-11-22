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

        logger.Information("Database is {Path}", Path.GetFullPath(db.Database.GetDbConnection().DataSource));
    }

    public Task Initialize() => db.Database.MigrateAsync();

    /// <summary>
    /// Deletes all of the quotes for <paramref name="ship"/> and adds <paramref name="quotes"/> in their place.
    /// </summary>
    /// <param name="ship">The ship whose quotes to replace.</param>
    /// <param name="quotes">The new quotes.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task UpdateQuotes(Ship ship, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        logger.Information("Saving {Count} quotes for {Ship}", quotes.Count(), ship);

        // Remove the ship's existing quotes (EF Core doesn't support Sqlite's UPSERT). Note that this assumes no two
        // ships will have the same English name.
        db.Quotes.RemoveRange(await db.Quotes
            .Where(q => q.Source == Source.Kancolle && q.Translations.Any(t => t.Language == "en" && t.SpeakerName == ship.EnglishName))
            .ToListAsync(cancellationToken));

        // Add the new quotes.
        db.Quotes.AddRange(quotes);

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the English names of the ships already in the database.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task<IEnumerable<string>> GetShips(CancellationToken cancellationToken = default)
    {
        return await db.Quotes
            .Where(q => q.Source == Source.Kancolle)
            .Select(q => q.Translations["en"].SpeakerName)
            .ToListAsync(cancellationToken);
    }
}
