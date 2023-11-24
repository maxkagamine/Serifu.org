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

    public Task Initialize()
    {
        throw new NotImplementedException();
    }

    public Task DeleteQuotes(Source source, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddQuotes(IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IQueryable<Quote>> GetQuotes()
    {
        throw new NotImplementedException();
    }

    public Task<string> ImportAudioFile(string path, bool copy = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> DownloadAudioFile(string url, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
