using Serifu.Data;

namespace Serifu.Importer.Generic;

/// <summary>
/// Intermediary record representing a raw quote parsed from the dialogue files, which will be paired with its
/// translation, validated &amp; normalized, word-aligned, and have its audio file imported before being assigned to a
/// <see cref="Quote"/> as a finalized <see cref="Translation"/>.
/// </summary>
internal record ParsedQuoteTranslation
{
    private Language language;

    /// <summary>
    /// A key that will be used to pair translations of a quote, as they may be coming from separate files. If all
    /// instances' keys are of type <see cref="int"/>, it will be used in the <see cref="Quote.Id"/> as-is. This should
    /// be preferred over strings where possible.
    /// </summary>
    public required object Key { get; init; }

    /// <summary>
    /// The quote language. Cannot be <see cref="Language.Multilingual"/>.
    /// </summary>
    public required Language Language
    {
        get => language;
        init
        {
            if (value is Language.Multilingual || !Enum.IsDefined(value))
            {
                throw new ArgumentException($"Invalid language: {value}", nameof(value));
            }

            language = value;
        }
    }

    /// <summary>
    /// The translated quote. Will be normalized before being assigned to <see cref="Translation.Text"/> (e.g. removing
    /// wrapping quote marks).
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The relative audio file path within one of the <see cref="ParserOptions.AudioDirectories"/> corresponding to the
    /// language (possibly omitting the extension), or <see langword="null"/> if audio is not available for this quote
    /// or language.
    /// </summary>
    public required string? AudioFilePath { get; init; }

    /// <summary>
    /// The localized name of the character to whom this quote belongs. May be empty if unknown or generic.
    /// </summary>
    public string SpeakerName { get; init; } = "";

    /// <summary>
    /// A short, translated description of when this quote is spoken. May be empty if unknown or generic.
    /// </summary>
    public string Context { get; init; } = "";

    /// <summary>
    /// Translation notes, if any. Contains sanitized HTML.
    /// </summary>
    public string Notes { get; init; } = "";

    /// <summary>
    /// In the event of duplicate quotes with different speakers, the quote with the highest weight will be chosen.
    /// </summary>
    public int Weight { get; init; } = 1;
}
