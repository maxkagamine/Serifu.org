using System.ComponentModel.DataAnnotations;

namespace Serifu.Importer.Generic.Tsv;

internal class TsvParserOptions : ParserOptions
{
    /// <summary>
    /// The columns in the TSV. <see cref="TsvColumn.Key"/> and <see cref="TsvColumn.Text"/> are required.
    /// </summary>
    public List<TsvColumn> Columns { get; init; } = [];

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (!Columns.Contains(TsvColumn.Key))
        {
            yield return new ValidationResult($"{nameof(TsvColumn.Key)} column is required.", [nameof(Columns)]);
        }

        if (!Columns.Contains(TsvColumn.Text))
        {
            yield return new ValidationResult($"{nameof(TsvColumn.Text)} column is required.", [nameof(Columns)]);
        }
    }
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
