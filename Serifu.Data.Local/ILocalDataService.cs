using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Serifu.Data.Local;

public interface ILocalDataService
{
    /// <summary>
    /// Creates/migrates the database and creates the audio directory if it doesn't exist.
    /// </summary>
    Task Initialize();

    /// <summary>
    /// Clears all quotes for <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source whose quotes to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task DeleteQuotes(Source source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds <paramref name="quotes"/> to the database.
    /// </summary>
    /// <param name="quotes">The quotes to add.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="DbUpdateException"/>
    Task AddQuotes(IEnumerable<Quote> quotes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quotes in the database.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable"/> with <see cref="Quote.Translations"/> included.
    /// </returns>
    IQueryable<Quote> GetQuotes();

    /// <summary>
    /// Hashes the audio file and either moves or copies it into the local audio directory. If the same file already
    /// exists and <paramref name="copy"/> is <see langword="false"/>, deletes the file at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to the audio file.</param>
    /// <param name="copy">Whether to leave the original file as is.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// The new audio file path, relative to the audio directory. This will be the object name once uploaded to cloud
    /// storage.
    /// </returns>
    Task<string> ImportAudioFile(string path, bool copy = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and imports the audio file at <paramref name="url"/>.
    /// </summary>
    /// <param name="url">The audio file url.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// The new audio file path, relative to the audio directory. This will be the object name once uploaded to cloud
    /// storage.
    /// </returns>
    Task<string> DownloadAudioFile(string url, CancellationToken cancellationToken = default);
}
