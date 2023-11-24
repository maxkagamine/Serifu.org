using Microsoft.EntityFrameworkCore;

namespace Serifu.Data.Local;

public interface ILocalDataService
{
    /// <summary>
    /// Creates/migrates the database and creates the audio directory if it doesn't exist.
    /// </summary>
    Task Initialize();

    /// <summary>
    /// Deletes all quotes for <paramref name="source"/> and replaces them with <paramref name="quotes"/>.
    /// </summary>
    /// <param name="source">The source whose quotes are being imported or updated.</param>
    /// <param name="quotes">The new quotes to add.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="DbUpdateException"/>
    Task ReplaceQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quotes in the database.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable"/> with <see cref="Quote.Translations"/> included.
    /// </returns>
    IQueryable<Quote> GetQuotes();

    /// <summary>
    /// Hashes the audio file and either moves it into the local audio directory or, if it already exists, deletes
    /// the copy at <paramref name="tempPath"/>.
    /// </summary>
    /// <param name="tempPath">The path to the audio file.</param>
    /// <param name="originalName">The original filename or url.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="AudioFile"/> record.</returns>
    Task<AudioFile> ImportAudioFile(string tempPath, string originalName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and imports the audio file at <paramref name="url"/>. If <paramref name="useCache"/> is <see
    /// langword="true"/> and an <see cref="AudioFile.OriginalName"/> with the requested <paramref name="url"/> is found
    /// in the database, skips sending a request and returns a copy of that object instead.
    /// </summary>
    /// <param name="url">The audio file url.</param>
    /// <param name="useCache">Whether to reuse an already-downloaded file from the same <paramref name="url"/>.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// A new <see cref="AudioFile"/> record with <see cref="AudioFile.OriginalName"/> set to <paramref name="url"/>, or
    /// a clone of an existing one.
    /// </returns>
    /// <exception cref="HttpRequestException"/>
    Task<AudioFile> DownloadAudioFile(string url, bool useCache = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes audio files not linked to any <see cref="Translation"/>.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default);
}
