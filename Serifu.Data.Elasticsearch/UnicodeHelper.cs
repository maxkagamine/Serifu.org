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

using System.Text;

namespace Serifu.Data.Elasticsearch;

/// <summary>
/// Utility methods for detecting the Unicode script of a character (Hiragana, Katakana, Han/Kanji) or whether a string
/// contains those scripts. Patterns generated from <see href="https://www.unicode.org/Public/16.0.0/ucd/Scripts.txt"/>.
/// </summary>
/// <remarks>
/// <para>
///     The "JapaneseCharacters" regex I've been using in various places only actually includes the common blocks; while
///     this is sufficient for detecting if a given text is Japanese or not (even if a quote contains "rare" characters,
///     it'll surely have some normal kanji and kana too), here I need to be able to detect if a single character is a
///     kanji or not. In theory, that might include code points above 0xFFFF.
/// </para>
/// <para>
///     Unfortunately, .NET's Regex supports neither Unicode scripts (e.g. \p{IsHan}) nor character ranges either of
///     which involving UTF-16 surrogate pairs. The UnicodeRanges class also doesn't support non-BMP ranges. So my
///     solution here is to generate the necessary pattern matching myself -- which is likely faster than the regex
///     approach, anyway.
/// </para>
/// </remarks>
internal static class UnicodeHelper
{
    private static bool IsHiragana(Rune rune) => rune.Value is
        (>= 0x3041 and <= 0x3096) or      // HIRAGANA LETTER SMALL A..HIRAGANA LETTER SMALL KE
        (>= 0x309D and <= 0x309E) or      // HIRAGANA ITERATION MARK..HIRAGANA VOICED ITERATION MARK
        0x309F or                         // HIRAGANA DIGRAPH YORI
        (>= 0x1B001 and <= 0x1B11F) or    // HIRAGANA LETTER ARCHAIC YE..HIRAGANA LETTER ARCHAIC WU
        0x1B132 or                        // HIRAGANA LETTER SMALL KO
        (>= 0x1B150 and <= 0x1B152) or    // HIRAGANA LETTER SMALL WI..HIRAGANA LETTER SMALL WO
        0x1F200;                          // SQUARE HIRAGANA HOKA

    private static bool IsKatakana(Rune rune) => rune.Value is
        (>= 0x30A1 and <= 0x30FA) or      // KATAKANA LETTER SMALL A..KATAKANA LETTER VO
        (>= 0x30FD and <= 0x30FE) or      // KATAKANA ITERATION MARK..KATAKANA VOICED ITERATION MARK
        0x30FF or                         // KATAKANA DIGRAPH KOTO
        (>= 0x31F0 and <= 0x31FF) or      // KATAKANA LETTER SMALL KU..KATAKANA LETTER SMALL RO
        (>= 0x32D0 and <= 0x32FE) or      // CIRCLED KATAKANA A..CIRCLED KATAKANA WO
        (>= 0x3300 and <= 0x3357) or      // SQUARE APAATO..SQUARE WATTO
        (>= 0xFF66 and <= 0xFF6F) or      // HALFWIDTH KATAKANA LETTER WO..HALFWIDTH KATAKANA LETTER SMALL TU
        (>= 0xFF71 and <= 0xFF9D) or      // HALFWIDTH KATAKANA LETTER A..HALFWIDTH KATAKANA LETTER N
        (>= 0x1AFF0 and <= 0x1AFF3) or    // KATAKANA LETTER MINNAN TONE-2..KATAKANA LETTER MINNAN TONE-5
        (>= 0x1AFF5 and <= 0x1AFFB) or    // KATAKANA LETTER MINNAN TONE-7..KATAKANA LETTER MINNAN NASALIZED TONE-5
        (>= 0x1AFFD and <= 0x1AFFE) or    // KATAKANA LETTER MINNAN NASALIZED TONE-7..KATAKANA LETTER MINNAN NASALIZED TONE-8
        0x1B000 or                        // KATAKANA LETTER ARCHAIC E
        (>= 0x1B120 and <= 0x1B122) or    // KATAKANA LETTER ARCHAIC YI..KATAKANA LETTER ARCHAIC WU
        0x1B155 or                        // KATAKANA LETTER SMALL KO
        (>= 0x1B164 and <= 0x1B167);      // KATAKANA LETTER SMALL WI..KATAKANA LETTER SMALL N

    private static bool IsHan(Rune rune) => rune.Value is
        (>= 0x2E80 and <= 0x2E99) or      // CJK RADICAL REPEAT..CJK RADICAL RAP
        (>= 0x2E9B and <= 0x2EF3) or      // CJK RADICAL CHOKE..CJK RADICAL C-SIMPLIFIED TURTLE
        (>= 0x2F00 and <= 0x2FD5) or      // KANGXI RADICAL ONE..KANGXI RADICAL FLUTE
        0x3005 or                         // IDEOGRAPHIC ITERATION MARK
        0x3007 or                         // IDEOGRAPHIC NUMBER ZERO
        (>= 0x3021 and <= 0x3029) or      // HANGZHOU NUMERAL ONE..HANGZHOU NUMERAL NINE
        (>= 0x3038 and <= 0x303A) or      // HANGZHOU NUMERAL TEN..HANGZHOU NUMERAL THIRTY
        0x303B or                         // VERTICAL IDEOGRAPHIC ITERATION MARK
        (>= 0x3400 and <= 0x4DBF) or      // CJK UNIFIED IDEOGRAPH-3400..CJK UNIFIED IDEOGRAPH-4DBF
        (>= 0x4E00 and <= 0x9FFF) or      // CJK UNIFIED IDEOGRAPH-4E00..CJK UNIFIED IDEOGRAPH-9FFF
        (>= 0xF900 and <= 0xFA6D) or      // CJK COMPATIBILITY IDEOGRAPH-F900..CJK COMPATIBILITY IDEOGRAPH-FA6D
        (>= 0xFA70 and <= 0xFAD9) or      // CJK COMPATIBILITY IDEOGRAPH-FA70..CJK COMPATIBILITY IDEOGRAPH-FAD9
        0x16FE2 or                        // OLD CHINESE HOOK MARK
        0x16FE3 or                        // OLD CHINESE ITERATION MARK
        (>= 0x16FF0 and <= 0x16FF1) or    // VIETNAMESE ALTERNATE READING MARK CA..VIETNAMESE ALTERNATE READING MARK NHAY
        (>= 0x20000 and <= 0x2A6DF) or    // CJK UNIFIED IDEOGRAPH-20000..CJK UNIFIED IDEOGRAPH-2A6DF
        (>= 0x2A700 and <= 0x2B739) or    // CJK UNIFIED IDEOGRAPH-2A700..CJK UNIFIED IDEOGRAPH-2B739
        (>= 0x2B740 and <= 0x2B81D) or    // CJK UNIFIED IDEOGRAPH-2B740..CJK UNIFIED IDEOGRAPH-2B81D
        (>= 0x2B820 and <= 0x2CEA1) or    // CJK UNIFIED IDEOGRAPH-2B820..CJK UNIFIED IDEOGRAPH-2CEA1
        (>= 0x2CEB0 and <= 0x2EBE0) or    // CJK UNIFIED IDEOGRAPH-2CEB0..CJK UNIFIED IDEOGRAPH-2EBE0
        (>= 0x2EBF0 and <= 0x2EE5D) or    // CJK UNIFIED IDEOGRAPH-2EBF0..CJK UNIFIED IDEOGRAPH-2EE5D
        (>= 0x2F800 and <= 0x2FA1D) or    // CJK COMPATIBILITY IDEOGRAPH-2F800..CJK COMPATIBILITY IDEOGRAPH-2FA1D
        (>= 0x30000 and <= 0x3134A) or    // CJK UNIFIED IDEOGRAPH-30000..CJK UNIFIED IDEOGRAPH-3134A
        (>= 0x31350 and <= 0x323AF);      // CJK UNIFIED IDEOGRAPH-31350..CJK UNIFIED IDEOGRAPH-323AF

    /// <summary>
    /// Checks whether <paramref name="str"/> contains hiragana, katakana, or kanji.
    /// </summary>
    /// <param name="str">The input span.</param>
    public static bool IsJapanese(ReadOnlySpan<char> str)
    {
        // TODO: Benchmark this against the regex used elsewhere and consider replacing the latter with this instead
        foreach (Rune rune in str.EnumerateRunes())
        {
            if (IsHiragana(rune) || IsKatakana(rune) || IsHan(rune))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether <paramref name="str"/> contains a single Unicode character in the Han (kanji) script ranges.
    /// </summary>
    /// <param name="str">The input span.</param>
    /// <remarks>
    /// This obviously includes characters not used in Japanese, but it matches what the kanji tokenizer does, and even
    /// if one searches for a "Vietnamese alternate reading mark," it'll just return no results. The alternative would
    /// be to build Unicode ranges for the kanji in JIS X 0213.
    ///
    /// Incidentally, there are 303 JIS kanji above 0xFFFF: https://x0213.org/codetable/jisx0213-2004-8bit-std.txt
    /// </remarks>
    public static bool IsSingleKanji(ReadOnlySpan<char> str)
    {
        if (str.Length is 0 or > 2) // .NET char is UTF-16, so a kanji could be two chars
        {
            return false;
        }

        // https://github.com/dotnet/runtime/issues/91513
        SpanRuneEnumerator enumerator = str.EnumerateRunes();
        enumerator.MoveNext();
        return enumerator.Current.Utf16SequenceLength == str.Length && IsHan(enumerator.Current);
    }
}
