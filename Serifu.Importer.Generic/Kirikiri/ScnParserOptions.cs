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
}
