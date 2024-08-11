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

namespace Serifu.Importer.Generic.Tsv;

internal class TsvParserOptions : ParserOptions
{
    /// <summary>
    /// The columns in the TSV. <see cref="TsvColumn.Key"/> and <see cref="TsvColumn.Text"/> are required.
    /// </summary>
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
