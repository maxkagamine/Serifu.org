using Microsoft.EntityFrameworkCore;

namespace Serifu.Data.Sqlite;

public interface ISqliteService
{
    /// <summary>
    /// Deletes all quotes for <paramref name="source"/> and replaces them with <paramref name="quotes"/>.
    /// </summary>
    /// <param name="source">The source whose quotes are being imported or updated.</param>
    /// <param name="quotes">The new quotes to add. <see cref="Quote.Source"/> must match <paramref
    /// name="source"/>.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="DbUpdateException"/>
    Task SaveQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if <paramref name="uri"/> has already been imported and returns its <see cref="AudioFile.ObjectName"/>.
    /// </summary>
    /// <param name="uri">
    /// The original audio file URL, if downloading from the web, or local file path, to avoid re-extracting/converting.
    /// <para/>
    /// File URIs should be constructed as "file:///{Source}/{PathRelativeToGameDir}#{PathInArchive}".
    /// </param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The object name, or <see langword="null"/> if not found.</returns>
    Task<string?> GetCachedAudioFile(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs an object name for the given audio file consisting of its hash and detected file extension and saves
    /// the file to the database if it does not already exist.
    /// <para/>
    /// If an <paramref name="originalUri"/> is provided, also saves a cache entry so that the next call to <see
    /// cref="GetCachedAudioFile(Uri, CancellationToken)"/> or <see cref="DownloadAudioFile(string,
    /// CancellationToken)"/> can retreive the object name and skip re-importing.
    /// </summary>
    /// <param name="stream">The file stream. Must be seekable.</param>
    /// <param name="originalUri">
    /// The original audio file URL, if downloading from the web, or local file path, to avoid re-extracting/converting.
    /// <para/>
    /// File URIs should be constructed as "file:///{Source}/{PathRelativeToGameDir}#{PathInArchive}".
    /// </param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The imported <see cref="AudioFile.ObjectName"/>.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="DbUpdateException"/>
    /// <exception cref="UnsupportedAudioFormatException"/>
    Task<string> ImportAudioFile(Stream stream, Uri? originalUri = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and imports the audio file at <paramref name="url"/> and returns its object name. If the file has
    /// already been downloaded, returns the cached object name instead.
    /// </summary>
    /// <param name="url">The remote audio file URL.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The imported <see cref="AudioFile.ObjectName"/>.</returns>
    /// <exception cref="UriFormatException"/>
    /// <exception cref="HttpRequestException"/>
    /// <exception cref="DbUpdateException"/>
    /// <exception cref="UnsupportedAudioFormatException"/>
    Task<string> DownloadAudioFile(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes audio files not linked to any <see cref="Translation"/>.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default);
}
