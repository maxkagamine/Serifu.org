using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Data.Local;
public class LocalDataService : ILocalDataService
{
    private readonly LocalDataOptions options;
    private readonly QuotesContext db;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public LocalDataService(
        IOptions<LocalDataOptions> options,
        QuotesContext db,
        HttpClient httpClient,
        ILogger logger)
    {
        this.options = options.Value;
        this.db = db;
        this.httpClient = httpClient;
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

    public async Task<AudioFile> ImportAudioFile(string tempPath, string originalName, CancellationToken cancellationToken = default)
    {
        logger.Information("Importing audio file {OriginalName} located at {TempPath}", originalName, tempPath);

        string extension = AudioFormatUtility.GetExtension(tempPath); // Throws if unsupported
        string hash = await ComputeHash(tempPath, cancellationToken);
        string path = CreateFilePath(hash, extension);
        string destPath = Path.GetFullPath(Path.Combine(options.AudioDirectory, path));

        if (File.Exists(destPath))
        {
            logger.Verbose("{Path} already exists, comparing files.", destPath);
            if (!CompareFiles(tempPath, destPath)) // Just to be sure
            {
                throw new Exception($"Holy crap a hash collision between \"{tempPath}\" and \"{destPath}\"!");
            }

            cancellationToken.ThrowIfCancellationRequested();

            logger.Verbose("Deleting {Path}", tempPath);
            File.Delete(tempPath);
        }
        else
        {
            logger.Verbose("Moving to {Path}", destPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Move(tempPath, destPath, overwrite: false);
        }

        return new AudioFile(path, originalName, File.GetLastWriteTime(tempPath));
    }

    public async Task<AudioFile> DownloadAudioFile(string url, bool useCache = true, CancellationToken cancellationToken = default)
    {
        if (useCache)
        {
            logger.Verbose("Checking if {Url} has already been downloaded.", url);

            var existing = await db.Quotes
                .SelectMany(q => q.Translations)
                .Where(t => t.AudioFile != null && t.AudioFile.OriginalName == url)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                logger.Information("Using cached audio file for {Url}", url);

                // Clone the record, as EF uses reference equality for owned entities
                return existing.AudioFile! with { };
            }
        }

        logger.Information("Downloading {Url}", url);

        string tempPath = Path.GetTempFileName();

        using (var stream = await httpClient.GetStreamAsync(url, cancellationToken))
        using (var file = File.OpenWrite(tempPath))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        return await ImportAudioFile(tempPath, url, cancellationToken);
    }

    public Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Computes the SHA1 hash of the given file.
    /// </summary>
    /// <remarks>
    /// SHA1 is not cryptographically secure, but it's fine for file hashing where there's no risk of malicious actors
    /// and where more secure hashes such as SHA-256 or SHA-3 would be too long.
    /// </remarks>
    /// <param name="filePath">The file to hash.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A lowercase hex string.</returns>
    private static async Task<string> ComputeHash(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha1 = SHA1.Create();
        using var file = File.OpenRead(filePath);

        var hash = await sha1.ComputeHashAsync(file, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Combines the <paramref name="hash"/> and <paramref name="extension"/> into a file path relative to the audio
    /// directory. This will be the object name in cloud storage.
    /// </summary>
    /// <param name="hash">The file hash, as a lowercase hex string.</param>
    /// <param name="extension">The file extension, lowercase without leading dot.</param>
    /// <returns>The file path, without any leading slash.</returns>
    private static string CreateFilePath(string hash, string extension)
    {
        return $"{hash[..2]}/{hash[2..4]}/{hash[4..]}.{extension}";
    }

    /// <summary>
    /// Performs a byte-for-byte comparison of two files.
    /// </summary>
    /// <param name="pathA">The first file path.</param>
    /// <param name="pathB">The second file path.</param>
    /// <returns><see langword="true"/> if the files are identical; otherwise, <see langword="false"/>.</returns>
    private static bool CompareFiles(string pathA, string pathB)
    {
        if (pathA == pathB)
        {
            return true;
        }

        using var fileA = File.OpenRead(pathA);
        using var fileB = File.OpenRead(pathB);

        if (fileA.Length != fileB.Length)
        {
            return false;
        }

        for (int i = 0; i < fileA.Length; i++)
        {
            int byteA = fileA.ReadByte();
            int byteB = fileB.ReadByte();

            if (byteA != byteB)
            {
                return false;
            }
        }

        return true;
    }
}
