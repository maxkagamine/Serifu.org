namespace Serifu.Importer.Generic.Kirikiri;

internal class ScnParserOptions : ParserOptions
{
    /// <summary>
    /// Index of Japanese text in the <c>.scenes[].texts[][2]</c> array.
    /// </summary>
    public int JapaneseLanguageIndex { get; set; } = 0;

    /// <summary>
    /// Index of English text in the <c>.scenes[].texts[][2]</c> array.
    /// </summary>
    public int EnglishLanguageIndex { get; set; } = 1;

    /// <summary>
    /// Whether to use <c>.scenes[].title</c> as the <see cref="ParsedQuoteTranslation.Context"/>.
    /// </summary>
    public bool UseSceneTitleAsContext { get; set; } = true;

    /// <summary>
    /// Replacement scene titles, per-language, where the key is the title as written in <c>.scenes[].title</c> for the
    /// given language and the value is the string to use instead.
    /// </summary>
    public Dictionary<Language, Dictionary<string, string>> SceneTitleMap { get; set; } = [];
}
