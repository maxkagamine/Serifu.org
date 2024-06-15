namespace Serifu.Data;

public record Translation
{
    /// <summary>
    /// The localized name of the character to whom this quote belongs. May be empty if unknown or generic.
    /// </summary>
    public required string SpeakerName { get; init; }

    /// <summary>
    /// A short, translated description of when this quote is spoken. May be empty if unknown or generic.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// The translated quote.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Translation notes, if any. Contains sanitized HTML.
    /// </summary>
    public string Notes { get; init; } = "";

    /// <summary>
    /// The audio file object name, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public string? AudioFile { get; init; }
}
