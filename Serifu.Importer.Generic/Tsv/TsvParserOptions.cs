// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using System.ComponentModel.DataAnnotations;

namespace Serifu.Importer.Generic.Tsv;

internal sealed class TsvParserOptions : ParserOptions
{
    /// <summary>
    /// The columns in the TSV. <see cref="TsvColumn.Text"/> and either <see cref="TsvColumn.IntKey"/> or <see
    /// cref="TsvColumn.StringKey"/> are required.
    /// </summary>
    public List<TsvColumn> Columns { get; init; } = [];

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (!Columns.Contains(TsvColumn.IntKey) && !Columns.Contains(TsvColumn.StringKey))
        {
            yield return new ValidationResult($"Either {nameof(TsvColumn.IntKey)} or {nameof(TsvColumn.StringKey)} column is required.", [nameof(Columns)]);
        }

        if (Columns.Contains(TsvColumn.IntKey) && Columns.Contains(TsvColumn.StringKey))
        {
            yield return new ValidationResult($"Cannot have both {nameof(TsvColumn.IntKey)} and {nameof(TsvColumn.StringKey)} columns.", [nameof(Columns)]);
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
    IntKey,
    StringKey,
    Language,
    Text,
    AudioFilePath,
    SpeakerName,
    Context,
    Notes,
}
