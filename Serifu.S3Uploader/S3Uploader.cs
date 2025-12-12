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

using Amazon.S3;
using Amazon.S3.Model;
using DotNext.Threading;
using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serifu.Data.Sqlite;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Serifu.S3Uploader;

internal sealed class S3Uploader : IAsyncDisposable
{
    // https://improve.dk/pushing-the-limits-of-amazon-s3-upload-performance/
    private const int ConcurrentUploads = 64;

    private readonly IAmazonS3 s3;
    private readonly IDbContextFactory<SerifuDbContext> dbFactory;
    private readonly AacAudioFallbackConverter aacAudioFallbackConverter;
    private readonly S3UploaderOptions options;
    private readonly ILogger logger;

    // I've had issues with sqlite multithreading in the past, so I'm playing it safe here and preventing any other db
    // access while a write is happening. Multiple threads can still read files concurrently, and the read lock will be
    // released as soon as the file is in memory (individual files are small).
    private readonly AsyncReaderWriterLock dbLock = new(concurrencyLevel: ConcurrentUploads);

    public S3Uploader(
        IAmazonS3 s3,
        IDbContextFactory<SerifuDbContext> dbFactory,
        AacAudioFallbackConverter aacAudioFallbackConverter,
        IOptions<S3UploaderOptions> options,
        ILogger logger)
    {
        this.s3 = s3;
        this.dbFactory = dbFactory;
        this.aacAudioFallbackConverter = aacAudioFallbackConverter;
        this.options = options.Value;
        this.logger = logger.ForContext<S3Uploader>();
    }

    public async Task PreDeploy(CancellationToken cancellationToken)
    {
        string[] audioFilesToUpload;
        await using (var db = await dbFactory.CreateDbContextAsync(cancellationToken))
        {
            audioFilesToUpload = await db.AudioFiles
                .Where(a => !db.S3ObjectCache.Any(c => c.Bucket == options.AudioBucket && c.ObjectName == a.ObjectName))
                .Select(a => a.ObjectName)
                .ToArrayAsync(cancellationToken);
        }

        int totalCount = audioFilesToUpload.Length;
        int startedCount = 0;
        int uploadedCount = 0;

        using var _ = logger.BeginTimedOperation("Uploading {Count} audio files to S3", totalCount);
        using var progress = new TerminalProgressBar();

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = ConcurrentUploads,
            CancellationToken = cancellationToken
        };

        await using var __ = cancellationToken.Register(() =>
            logger.Information("Canceled, waiting for threads to complete"));

        await Parallel.ForEachAsync(audioFilesToUpload, parallelOptions, async (objectName, cancellationToken) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            byte[] data;
            using (await dbLock.AcquireReadLockAsync(cancellationToken))
            {
                logger.Information("({Number} / {TotalCount}) Uploading {ObjectName} to {Bucket}",
                    Interlocked.Increment(ref startedCount), totalCount, objectName, options.AudioBucket);

                data = (byte[])await db.AudioFiles.Where(a => a.ObjectName == objectName)
                    .Select(a => a.Data)
                    .SingleAsync(cancellationToken);
            }

            using var stream = new MemoryStream(data);

            await Upload(objectName, stream, db, cancellationToken);

            // Upload AAC fallback for Safari (see comment in converter)
            if (Path.GetExtension(objectName) != ".mp3")
            {
                stream.Position = 0;
                string aacObjectName = Path.ChangeExtension(objectName, ".m4a");
                await using Stream aacStream = await aacAudioFallbackConverter.ConvertToAac(stream, cancellationToken);

                await Upload(aacObjectName, aacStream, db, cancellationToken);
            }

            using (await dbLock.AcquireWriteLockAsync(CancellationToken.None))
            {
                await db.SaveChangesAsync(CancellationToken.None);
                progress.SetProgress(++uploadedCount, totalCount);
            }
        });
    }

    private async Task Upload(string objectName, Stream stream, SerifuDbContext db, CancellationToken cancellationToken)
    {
        PutObjectResponse response = await s3.PutObjectAsync(
            new PutObjectRequest()
            {
                BucketName = options.AudioBucket,
                Key = objectName,
                ContentType = Path.GetExtension(objectName) switch
                {
                    // See also the content type list in Serifu.Web and related logic in AudioFormatUtility
                    ".mp3" => "audio/mp3",
                    ".ogg" or ".opus" => "audio/ogg",
                    ".m4a" => "audio/mp4",
                    string ext => throw new UnreachableException($"No content type defined for {ext}")
                },
                InputStream = stream,
                AutoCloseStream = false
            },
            cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK) // Docs don't make it clear if it'll always throw or not
        {
            throw new AmazonS3Exception($"Upload failed with status code {response.HttpStatusCode}.");
        }

        db.S3ObjectCache.Add(new(options.AudioBucket, objectName, stream.Length));
    }

    [SuppressMessage("Performance", "CA1866:Use char overload", Justification = "not translatable to SQL warning")]
    public async Task PostDeploy(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        string[] audioFilesToDelete = await db.S3ObjectCache
            .Where(c => c.Bucket == options.AudioBucket && !db.AudioFiles
                .Select(a => a.ObjectName.Substring(0, a.ObjectName.IndexOf(".")))
                .Contains(c.ObjectName.Substring(0, c.ObjectName.IndexOf("."))))
            .Select(c => c.ObjectName)
            .ToArrayAsync(cancellationToken);

        using var _ = logger.BeginTimedOperation("Deleting {Count} old files from {Bucket}",
            audioFilesToDelete.Length, options.AudioBucket);

        string[][] batches = audioFilesToDelete.Chunk(1000).ToArray();

        for (int i = 0; i < batches.Length; i++)
        {
            string[] batch = batches[i];
            logger.Information("Batch {Number} of {Count}", i + 1, batches.Length);

            DeleteObjectsResponse response = await s3.DeleteObjectsAsync(
                new DeleteObjectsRequest()
                {
                    BucketName = options.AudioBucket,
                    Objects = batch.Select(x => new KeyVersion() { Key = x }).ToList()
                },
                cancellationToken);

            if (response.HttpStatusCode != HttpStatusCode.OK) // Docs don't make it clear if it'll always throw or not
            {
                throw new AmazonS3Exception($"Delete failed with status code {response.HttpStatusCode}.");
            }

            await db.S3ObjectCache
                .Where(c => c.Bucket == options.AudioBucket && Enumerable.Contains(batch, c.ObjectName)) // https://github.com/dotnet/runtime/issues/109757
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await dbLock.DisposeAsync();
    }
}
