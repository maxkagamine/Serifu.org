using System.Diagnostics;

namespace Serifu.Data;

[DebuggerDisplay("Language = {Language}, Text = {Text}")]
public class Translation
{
    /// <summary>
    /// Two-letter language code.
    /// </summary>
    public required string Language { get; set; }

    /// <summary>
    /// The localized name of the character to whom this quote belongs. May be empty if unknown or generic.
    /// </summary>
    public required string SpeakerName { get; set; }

    /// <summary>
    /// A short, translated description of when this quote is spoken. May be empty if unknown or generic.
    /// </summary>
    public required string Context { get; set; }

    /// <summary>
    /// The translated quote.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Translation notes, if any. Contains sanitized HTML.
    /// </summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// The audio file, or <see langword="null"/> if audio is not available for this quote or language.
    /// </summary>
    public AudioFile? AudioFile { get; set; }
}
