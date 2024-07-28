namespace Serifu.Importer.Skyrim;

public class SkyrimOptions
{
    /// <summary>
    /// Absolute path to the game's Data directory.
    /// </summary>
    public required string DataDirectory { get; set; }

    /// <summary>
    /// Absolute path to the archive containing English voice files.
    /// </summary>
    public required string EnglishVoiceBsaPath { get; set; }

    /// <summary>
    /// Absolute path to the archive containing Japanese voice files.
    /// </summary>
    public required string JapaneseVoiceBsaPath { get; set; }

    /// <summary>
    /// A map of faction editor IDs to list of either NPC names (matches all NPCs in the faction with the exact English
    /// name) or FormKeys (for when the NPC may not exist in the faction by default) to prioritize over the rest of the
    /// faction when it is used as part of a non-negated GetInFaction condition.
    /// </summary>
    public Dictionary<string, List<string>> FactionOverrides { get; set; } = [];
}
