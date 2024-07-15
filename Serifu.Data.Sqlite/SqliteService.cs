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

using Kagamine.Extensions.Collections;
using Kagamine.Extensions.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Cryptography;

namespace Serifu.Data.Sqlite;

public class SqliteService : ISqliteService
{
    private readonly IDbContextFactory<SerifuDbContext> dbFactory;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public SqliteService(IDbContextFactory<SerifuDbContext> dbFactory, HttpClient httpClient, ILogger logger)
    {
        this.dbFactory = dbFactory;
        this.httpClient = httpClient;
        this.logger = logger.ForContext<SqliteService>();
    }

    public async Task SaveQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        logger.Information("Saving {Count} quotes for {Source}.", quotes.Count(), source);

        // This avoids having to fetch all existing entities just so we can delete them, but it has to be done in a
        // transaction because ExecuteDeleteAsync will execute immediately (there's no way to delete via SaveChanges
        // without having a reference to an entity)
        using var db = dbFactory.CreateDbContext();
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var existingIds = await db.Quotes
            .Where(q => q.Source == source)
            .Select(s => s.Id)
            .ToHashSetAsync(cancellationToken);

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
        using var db = dbFactory.CreateDbContext();
        return (await db.AudioFileCache.FindAsync([uri], cancellationToken))?.ObjectName;
    }

    public async Task<string> ImportAudioFile(Stream stream, Uri? originalUri = null, CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream is not seekable.", nameof(stream));
        }

        using var db = dbFactory.CreateDbContext();

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
                Data = ReadStreamAsValueArray(stream)
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
                db.AudioFileCache.Update(cacheEntry);
            }
            else
            {
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

        string? objectName = await GetCachedAudioFile(uri, cancellationToken);
        if (objectName is not null)
        {
            // TODO: Add an option to try downloading with If-Modified-Since. The kancolle wiki doesn't appear to
            // support this (it gives us a Last-Modified and ETag but never responds with 304).
            logger.Debug("Using cached audio file {ObjectName} for {Url}", objectName, url);
            return objectName;
        }

        logger.Information("Downloading {Url}", url);

        HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        // If we got a content length, initialize the memory stream to that capacity. HttpClient.GetByteArrayAsync()
        // does the same thing, but it lets us have a stream but also use its underlying byte array without copying.
        long? contentLength = response.Content.Headers.ContentLength;
        using var stream = new MemoryStream(checked((int)contentLength.GetValueOrDefault()));

        await response.Content.CopyToAsync(stream, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        return await ImportAudioFile(stream, uri, cancellationToken);
    }

    public async Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        using var db = dbFactory.CreateDbContext();

        HashSet<string> referencedAudioFiles = (await db.Quotes
            .Select(q => new { English = q.English.AudioFile, Japanese = q.Japanese.AudioFile })
            .ToArrayAsync(cancellationToken))
            .SelectMany(x => new[] { x.English, x.Japanese })
            .OfType<string>()
            .ToHashSet();

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

    private static ValueArray<byte> ReadStreamAsValueArray(Stream stream)
    {
        // Read the stream into memory if it's not already a MemoryStream (probably a FileStream). Since we expect the
        // stream to be seekable, we can initialize the internal buffer with the correct length which allows the below
        // logic to directly slide it into an ValueArray.
        if (stream is not MemoryStream mem)
        {
            using var mem2 = new MemoryStream(checked((int)stream.Length));
            stream.CopyTo(mem2);
            return ReadStreamAsValueArray(mem2);
        }

        // Minimize allocations by using the internal buffer if possible (same as HttpClient's internal GetSizedBuffer)
        //
        // WARNING: For performance we intentionally do not create a defensive copy. This is dangerous because it relies
        // on the caller not passing us a MemoryStream they intend to modify; otherwise the buffer could be modified
        // externally which would mutate the underlying array. If this were part of a library or larger team project,
        // we'd need to copy the array first.
        return mem.TryGetBuffer(out ArraySegment<byte> buffer) &&
            buffer.Offset == 0 && buffer.Count == buffer.Array!.Length ? buffer.Array! : mem.ToArray();
    }
}
