using System.Text.RegularExpressions;

namespace Serifu.Data.Sqlite;

public static partial class ImportHelper
{
    /// <summary>
    /// Trims whitespace and wrapping quotes and collapses newlines &amp; spaces into a single space.
    /// </summary>
    /// <param name="text">The English text.</param>
    /// <returns>The English text ready to be word-aligned and used in a <see cref="Quote"/>.</returns>
    public static string FormatEnglishText(string text)
    {
        text = TrimQuoteText(text);
        return WhitespaceRegex.Replace(text, " ");
    }

    /// <summary>
    /// Trims whitespace and wrapping quotes and removes newlines.
    /// </summary>
    /// <param name="text">The Japanese text.</param>
    /// <returns>The Japanese text ready to be word-aligned and used in a <see cref="Quote"/>.</returns>
    public static string FormatJapaneseText(string text)
    {
        text = TrimQuoteText(text);
        return NewlinesRegex.Replace(text, "");
    }

    /// <summary>
    /// Checks if <paramref name="text"/> contains kanji or hiragana. Useful for checking if the Japanese text actually
    /// contains Japanese (to catch errors / untranslated lines) and isn't just katakana (to filter out grunts and SFX).
    /// </summary>
    /// <param name="text">The Japanese text to search.</param>
    public static bool ContainsKanjiOrHiragana(string text) => KanjiOrHiraganaRegex.IsMatch(text);

    private static string TrimQuoteText(ReadOnlySpan<char> text)
    {
        text = text.Trim();

        // This could mishandle text like: "Blah," said blah. "Blah!"
        if (text.Length > 0 && text[0] is '"' or '“' or '「' or '『' && text[^1] is '"' or '”' or '」' or '』')
        {
            text = text[1..^1].Trim();
        }

        return text.ToString();
    }

    [GeneratedRegex(@"[一-龠ぁ-ゔ]")]
    private static partial Regex KanjiOrHiraganaRegex { get; }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex { get; }

    [GeneratedRegex(@"\r|\n")]
    private static partial Regex NewlinesRegex { get; }
}
