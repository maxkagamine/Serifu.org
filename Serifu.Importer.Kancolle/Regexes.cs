﻿using System.Text.RegularExpressions;

namespace Serifu.Importer.Kancolle;

internal static partial class Regexes
{
    [GeneratedRegex(@"\d+(st|nd|rd|th)|(?! )[^\d():/]+(?<! )")]
    public static partial Regex ContextTokenizer { get; }

    [GeneratedRegex(@"[一-龠ぁ-ゔァ-ヴー々〆〤ヶ]")]
    public static partial Regex JapaneseCharacters { get; }

    [GeneratedRegex(@"(?<=[一-龠ぁ-ゔァ-ヴー々〆〤ヶ])\s+(?=[一-龠ぁ-ゔァ-ヴー々〆〤ヶ])")]
    public static partial Regex SpacesBetweenJapaneseCharacters { get; }

    [GeneratedRegex(@"^[\s\?]*$")]
    public static partial Regex EmptyOrQuestionMarks { get; }

    [GeneratedRegex(@"\s*/\s*")]
    public static partial Regex Slash { get; }

    [GeneratedRegex(@"(?<=[a-z])[A-Z0-9]")]
    public static partial Regex FirstCharacterOfPascalCaseWord { get; }

    [GeneratedRegex(@"^.|(?<=[ ([]).")]
    public static partial Regex FirstCharacterOfWord { get; }

    [GeneratedRegex(@" (a|an|and|as|at|but|by|for|if|in|nor|of|off|on|or|per|so|the|to|up|via|yet)(?= )", RegexOptions.IgnoreCase)]
    public static partial Regex TitleCaseLowercaseWords { get; }

    [GeneratedRegex(@"Docking \(?(Minor|Major)(?: Damage)?\)?")]
    public static partial Regex DockingMajorMinorDamage { get; }

    [GeneratedRegex(@"(?<=\d{4}) (Mini-)?Event|(Mini-)?Event (?=\d{4})")]
    public static partial Regex EventNextToYear { get; }

    [GeneratedRegex(@"(?<=(st|nd|rd|th) Anniversary) \d{4}")]
    public static partial Regex YearNextToAnniversary { get; }

    [GeneratedRegex(@"^Special(?! ?[A-Za-z])")]
    public static partial Regex JustSpecial { get; }

    [GeneratedRegex(@"(?<!\(.*)\s\d\b")]
    public static partial Regex SingleDigitNumberAfterContext { get; }
}
