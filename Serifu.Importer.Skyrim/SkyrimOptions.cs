namespace Serifu.Importer.Skyrim;

public class SkyrimOptions
{
    /// <summary>
    /// Absolute path to the game's Data directory.
    /// </summary>
    public required string DataDirectory { get; init; }

    /// <summary>
    /// Absolute path to the archive containing English voice files.
    /// </summary>
    public required string EnglishVoiceBsaPath { get; init; }

    /// <summary>
    /// Absolute path to the archive containing Japanese voice files.
    /// </summary>
    public required string JapaneseVoiceBsaPath { get; init; }
}
