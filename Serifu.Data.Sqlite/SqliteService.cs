using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Serifu.Data.Sqlite;

public class SqliteService : ISqliteService
{
    private readonly SerifuContext db;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public SqliteService(SerifuContext db, HttpClient httpClient, ILogger logger)
    {
        this.db = db;
        this.httpClient = httpClient;
        this.logger = logger.ForContext<SqliteService>();
    }

    public async Task SaveQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        // This avoids having to fetch all existing entities just so we can delete them, but it has to be done in a
        // transaction because ExecuteDeleteAsync will execute immediately (there's no way to delete via SaveChanges
        // without having a reference to an entity)
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var existingIds = (await db.Quotes.Select(s => s.Id).ToArrayAsync(cancellationToken)).ToHashSet();

        foreach (var quote in quotes)
        {
            if (existingIds.Remove(quote.Id))
            {
                db.Quotes.Update(quote);
            }
            else
            {
                db.Quotes.Add(quote);
            }
        }

        await db.Quotes.Where(q => existingIds.Contains(q.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<string?> GetCachedAudioFile(Uri uri, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> ImportAudioFile(Stream stream, Uri? originalUri = null, CancellationToken cancellationToken = default)
    {
        // TODO: Should this method require a seekable stream, since then we can potentially create a Content-Length
        // sized MemoryStream in DownloadAudioFile and leverage TryGetBuffer the way GetByteArrayAsyncCore does? At some
        // point we need to turn the stream into a byte array, but working with streams may be a bit nicer (might even
        // be a good idea to interact with the sqlar table via ADO.NET the way sqlarserver does, and skip the whole EF
        // byte[] mess altogether...)
        // TODO: Remember to add the unsupported audio exception to the xml doc
        throw new NotImplementedException();
    }

    public async Task<string> DownloadAudioFile(string uri, CancellationToken cancellationToken = default)
    {
        // TODO: Remember to add the unsupported audio exception to the xml doc
        throw new NotImplementedException();
    }

    public async Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
