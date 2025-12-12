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

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Serifu.Importer.Generic.Tsv;

internal sealed class TsvParser : IParser<TsvParserOptions>
{
    private readonly TsvParserOptions options;

    public TsvParser(IOptions<TsvParserOptions> options)
    {
        this.options = options.Value;
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        if (language is Language.Multilingual && !options.Columns.Contains(TsvColumn.Language))
        {
            throw new ValidationException($"When reading a multilingual TSV, one of the columns must be the {nameof(TsvColumn.Language)}.");
        }

        using StreamReader reader = File.OpenText(path);

        string? line;
        int lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            ParsedQuoteTranslation tl;

            try
            {
                string[] row = line.Split('\t');

                tl = new()
                {
                    Key = options.Columns.Contains(TsvColumn.IntKey) ?
                        GetColumn<int>(row, TsvColumn.IntKey) : GetColumn<string>(row, TsvColumn.StringKey, ""),
                    Language = language is Language.Multilingual ?
                        Enum.Parse<Language>(GetColumn(row, TsvColumn.Language, "")) : language,
                    Text = GetColumn(row, TsvColumn.Text, ""),
                    AudioFilePath = GetColumn<string>(row, TsvColumn.AudioFilePath),
                    SpeakerName = GetColumn(row, TsvColumn.SpeakerName, ""),
                    Context = GetColumn(row, TsvColumn.Context, ""),
                    Notes = GetColumn(row, TsvColumn.Notes, "")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error parsing line {lineNumber} of {path}", ex);
            }

            yield return tl;
        }
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    private T? GetColumn<T>(string[] row, TsvColumn column, T? defaultValue = default)
        where T : IParsable<T>
    {
        int columnIndex = options.Columns.IndexOf(column);
        if (columnIndex < 0 || !T.TryParse(row[columnIndex], null, out T? value))
        {
            return defaultValue;
        }

        return value;
    }
}
