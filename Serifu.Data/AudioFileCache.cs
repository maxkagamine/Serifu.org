namespace Serifu.Data;

/// <summary>
/// Used to keep track of already-imported audio files.
/// </summary>
public record AudioFileCache
{
    /// <summary>
    /// The original audio file URL, if downloading from the web, or local file path, to avoid re-extracting/converting.
    /// </summary>
    /// <remarks>
    /// File URIs should be constructed as "file:///{Source}/{PathRelativeToGameDir}#{PathInArchive}".
    /// </remarks>
    public required Uri OriginalUri { get; init; }

    /// <summary>
    /// The already-imported <see cref="AudioFile.ObjectName"/>.
    /// </summary>
    public required string ObjectName { get; init; }
}
