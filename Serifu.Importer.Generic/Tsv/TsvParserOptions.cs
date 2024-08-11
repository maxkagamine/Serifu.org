namespace Serifu.Importer.Generic.Tsv;

internal class TsvParserOptions : ParserOptions
{
    /// <summary>
    /// The columns in the TSV. <see cref="TsvColumn.Key"/> and <see cref="TsvColumn.Text"/> are required.
    /// </summary>
    /// <remarks>
    /// Note: the current implementation expects the key to be a 32-bit integer in decimal form.
    /// </remarks>
    public List<TsvColumn> Columns { get; init; } = [];
}

/// <summary>
/// Columns corresponding to properties of <see cref="ParsedQuoteTranslation"/>.
/// </summary>
internal enum TsvColumn
{
    Key,
    Language,
    Text,
    AudioFilePath,
    SpeakerName,
    Context,
    Notes,
}
