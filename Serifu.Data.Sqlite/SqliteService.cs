using Kagamine.Extensions.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
        logger.Information("Saving {Count} quotes for {Source}.", quotes.Count(), source);

        // This avoids having to fetch all existing entities just so we can delete them, but it has to be done in a
        // transaction because ExecuteDeleteAsync will execute immediately (there's no way to delete via SaveChanges
        // without having a reference to an entity)
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var existingIds = await db.Quotes.Select(s => s.Id).ToHashSetAsync(cancellationToken);

        foreach (var quote in quotes)
        {
            if (quote.Source != source) // Sanity check
            {
                throw new ArgumentException($"Quote does not belong to source {source}: {quote}", nameof(quotes));
            }

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
        return (await db.AudioFileCache.FindAsync([uri], cancellationToken))?.ObjectName;
    }

    public async Task<string> ImportAudioFile(Stream stream, Uri? originalUri = null, CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream is not seekable.", nameof(stream));
        }

        // Compose the object name; this will throw if it's not a supported audio format
        string objectName = await CreateObjectName(stream, cancellationToken);

        // Save the audio file to the database if it doesn't exist (these are SHA1 hashes; malicious actors aren't a
        // concern here so we can realistically assume no collisions)
        bool audioFileExists = await db.AudioFiles.AnyAsync(a => a.ObjectName == objectName, cancellationToken);

        if (audioFileExists)
        {
            logger.Information("Audio file {ObjectName} already imported.", objectName);
        }
        else
        {
            logger.Information("Saving audio file {ObjectName} to database.", objectName);

            var audioFile = new AudioFile()
            {
                ObjectName = objectName,
                Data = ReadStreamToImmutableByteArray(stream)
            };

            db.AudioFiles.Add(audioFile);
        }
        
        // Update the cache if we were given a URI
        if (originalUri is not null)
        {
            var uriHasExistingCacheEntry = await db.AudioFileCache.AnyAsync(c => c.OriginalUri == originalUri, cancellationToken);
            var cacheEntry = new AudioFileCache()
            {
                OriginalUri = originalUri,
                ObjectName = objectName
            };

            if (uriHasExistingCacheEntry)
            {
                logger.Debug("Updating cache entry {OriginalUri} -> {ObjectName}", originalUri, objectName);

                db.AudioFileCache.Update(cacheEntry);
            }
            else
            {
                logger.Debug("Saving cache entry {OriginalUri} -> {ObjectName}", originalUri, objectName);

                db.AudioFileCache.Add(cacheEntry);
            }
        }

        // Whoop
        await db.SaveChangesAsync(cancellationToken);
        return objectName;
    }

    public async Task<string> DownloadAudioFile(string url, CancellationToken cancellationToken = default)
    {
        Uri uri = new(url, UriKind.Absolute);

        logger.Information("Checking if {Url} has already been downloaded.", url);

        string? objectName = await GetCachedAudioFile(uri, cancellationToken);
        if (objectName is not null)
        {
            // TODO: Add an option to try downloading with If-Modified-Since. The kancolle wiki doesn't appear to
            // support this (it gives us a Last-Modified and ETag but never responds with 304).
            logger.Information("Using cached audio file {ObjectName}", objectName);
        }

        logger.Information("Downloading {Url}", url);

        HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);

        // If we got a content length, initialize the memory stream to that capacity. HttpClient.GetByteArrayAsync()
        // does the same thing, but it lets us have a stream but also use its underlying byte array without copying.
        long? contentLength = response.Content.Headers.ContentLength;
        using var stream = new MemoryStream(checked((int)contentLength.GetValueOrDefault()));

        await response.Content.CopyToAsync(stream, cancellationToken);

        return await ImportAudioFile(stream, uri, cancellationToken);
    }

    public async Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        var referencedAudioFiles = await db.Quotes
            .SelectMany(q => new[] { q.English.AudioFile, q.Japanese.AudioFile })
            .Where(a => a != null)
            .ToHashSetAsync(cancellationToken);

        var audioFilesInDb = await db.AudioFiles.Select(a => a.ObjectName).ToListAsync(cancellationToken);
        var audioFilesToDelete = new List<string>();

        foreach (string audioFile in audioFilesInDb)
        {
            if (!referencedAudioFiles.Contains(audioFile))
            {
                logger.Information("Deleting orphaned audio file {ObjectName}", audioFile);
                audioFilesInDb.Add(audioFile);
            }
        }

        await db.AudioFiles.Where(a => audioFilesToDelete.Contains(a.ObjectName)).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Formats an object name for the given audio file using its hash and detected file type.
    /// </summary>
    /// <param name="audioFileStream">The audio file stream. Must be seekable.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="UnsupportedAudioFormatException"/>
    private static async Task<string> CreateObjectName(Stream audioFileStream, CancellationToken cancellationToken)
    {
        // Determine file extension
        string ext = AudioFormatUtility.GetExtension(audioFileStream);

        audioFileStream.Seek(0, SeekOrigin.Begin);

        // Compute hash (SHA1 is not cryptographically secure, but it's fine for this sort of file hashing where there's
        // no risk of malicious actors & more secure hashes such as SHA-256 or SHA-3 would be too long)
        using var sha1 = SHA1.Create();

        byte[] hashBytes = await sha1.ComputeHashAsync(audioFileStream, cancellationToken);
        string hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        audioFileStream.Seek(0, SeekOrigin.Begin);

        // Format object name
        return $"{hash[..2]}/{hash[2..4]}/{hash[4..]}.{ext}";
    }

    private static ImmutableArray<byte> ReadStreamToImmutableByteArray(Stream stream)
    {
        // Read the stream into memory if it's not already a MemoryStream (probably a FileStream). Since we expect the
        // stream to be seekable, we can initialize the internal buffer with the correct length which allows the below
        // logic to directly slide it into an ImmutableArray.
        if (stream is not MemoryStream mem)
        {
            using var mem2 = new MemoryStream(checked((int)stream.Length));
            stream.CopyTo(mem2);
            return ReadStreamToImmutableByteArray(mem2);
        }

        // Minimize allocations by using the internal buffer if possible (same as HttpClient's internal GetSizedBuffer)
        byte[] data = mem.TryGetBuffer(out ArraySegment<byte> buffer) &&
            buffer.Offset == 0 && buffer.Count == buffer.Array!.Length ? buffer.Array! : mem.ToArray();

        // Construct an ImmutableArray backed by this array.
        //
        // WARNING: This is dangerous because it relies on the caller not passing us a MemoryStream they intend to
        // modify; otherwise the buffer could be modified externally which would mutate the immutable array. If this
        // were part of a public library or larger team project, we'd need to use the ctor and create a defensive copy.
        return ImmutableCollectionsMarshal.AsImmutableArray(data);
    }
}
