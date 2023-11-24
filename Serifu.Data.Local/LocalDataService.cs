using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Data.Local;
public class LocalDataService : ILocalDataService
{
    private readonly LocalDataOptions options;
    private readonly QuotesContext db;
    private readonly ILogger logger;

    public LocalDataService(IOptions<LocalDataOptions> options, QuotesContext db, ILogger logger)
    {
        this.options = options.Value;
        this.db = db;
        this.logger = logger.ForContext<LocalDataService>();

        logger.Information("Database is {Path}", Path.GetFullPath(db.Database.GetDbConnection().DataSource));
        logger.Information("Audio directory is {Path}", Path.GetFullPath(options.Value.AudioDirectory));
    }

    public async Task Initialize()
    {
        await db.Database.MigrateAsync();
        Directory.CreateDirectory(options.AudioDirectory);
    }

    public async Task ReplaceQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        logger.Information("Saving {Count} quotes for {Source}.", quotes.Count(), source);

        db.Quotes.RemoveRange(db.Quotes.Where(q => q.Source == source));
        db.Quotes.AddRange(quotes);
        await db.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Quote> GetQuotes()
    {
        return db.Quotes.Include(q => q.Translations);
    }

    public Task<AudioFile> ImportAudioFile(string tempPath, string originalName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<AudioFile> DownloadAudioFile(string url, bool useCache = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
