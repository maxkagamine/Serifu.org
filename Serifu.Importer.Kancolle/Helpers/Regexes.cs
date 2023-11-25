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

using System.Text.RegularExpressions;

namespace Serifu.Importer.Kancolle.Helpers;
internal static partial class Regexes
{
    [GeneratedRegex(@"\d+(st|nd|rd|th)|(?! )[^\d():/]+(?<! )")]
    private static partial Regex ContextTokenizerRegex();

    [GeneratedRegex(@"(?<=[一-龠ぁ-ゔァ-ヴー々〆〤ヶ])\s+(?=[一-龠ぁ-ゔァ-ヴー々〆〤ヶ])")]
    private static partial Regex SpacesBetweenJapaneseCharactersRegex();

    [GeneratedRegex(@"^[\s\?]*$")]
    private static partial Regex EmptyOrQuestionMarksRegex();

    [GeneratedRegex(@"\s*/\s*")]
    private static partial Regex SlashRegex();

    [GeneratedRegex(@"(?<=[a-z])[A-Z0-9]")]
    private static partial Regex PascalCasedLettersRegex();

    [GeneratedRegex(@"^.|(?<=[ ([]).")]
    private static partial Regex FirstCharacterOfWordRegex();

    [GeneratedRegex(@" (a|an|and|as|at|but|by|for|if|in|nor|of|off|on|or|per|so|the|to|up|via|yet)(?= )", RegexOptions.IgnoreCase)]
    private static partial Regex LowercaseWordsRegex();

    [GeneratedRegex(@"Docking \(?(Minor|Major)(?: Damage)?\)?")]
    private static partial Regex DockingMajorMinorDamageRegex();

    [GeneratedRegex(@"(?<=\d{4}) (Mini-)?Event|(Mini-)?Event (?=\d{4})")]
    private static partial Regex EventNextToYearRegex();

    [GeneratedRegex(@"(?<=(st|nd|rd|th) Anniversary) \d{4}")]
    private static partial Regex YearNextToAnniversaryRegex();

    /// <inheritdoc cref="ContextTokenizerRegex"/>
    public static Regex ContextTokenizer => ContextTokenizerRegex();

    /// <inheritdoc cref="SpacesBetweenJapaneseCharactersRegex"/>
    public static Regex SpacesBetweenJapaneseCharacters => SpacesBetweenJapaneseCharactersRegex();

    /// <inheritdoc cref="EmptyOrQuestionMarksRegex"/>
    public static Regex EmptyOrQuestionMarks => EmptyOrQuestionMarksRegex();

    /// <inheritdoc cref="SlashRegex"/>
    public static Regex Slash => SlashRegex();

    /// <inheritdoc cref="PascalCasedLettersRegex"/>
    public static Regex PascalCasedLetters => PascalCasedLettersRegex();

    /// <inheritdoc cref="FirstCharacterOfWordRegex"/>
    public static Regex FirstCharacterOfWord => FirstCharacterOfWordRegex();

    /// <inheritdoc cref="LowercaseWordsRegex"/>
    public static Regex LowercaseWords => LowercaseWordsRegex();

    /// <inheritdoc cref="DockingMajorMinorDamageRegex"/>
    public static Regex DockingMajorMinorDamage => DockingMajorMinorDamageRegex();

    /// <inheritdoc cref="EventNextToYearRegex"/>
    public static Regex EventNextToYear => EventNextToYearRegex();

    /// <inheritdoc cref="YearNextToAnniversaryRegex"/>
    public static Regex YearNextToAnniversary => YearNextToAnniversaryRegex();
}
