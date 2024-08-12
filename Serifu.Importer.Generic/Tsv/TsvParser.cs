using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Serifu.Importer.Generic.Tsv;

internal class TsvParser : IParser<TsvParserOptions>
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
                    // Note: Current impl expects the key to be an int in decimal form.
                    Key = GetColumn<int>(row, TsvColumn.Key),
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
