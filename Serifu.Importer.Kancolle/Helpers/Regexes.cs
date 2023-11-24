using System.Text.RegularExpressions;

namespace Serifu.Importer.Kancolle.Helpers;
internal static partial class Regexes
{
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
}
