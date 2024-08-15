namespace Serifu.Importer.Generic.CatSystem2;

internal class CstParserOptions : ParserOptions
{
    /// <summary>
    /// Dump each scene to a readable format for analysis.
    /// </summary>
    public bool DumpCst { get; set; }

    /// <summary>
    /// Lines with any of these audio files (sans extension) will be dropped. Used to remove incorrect translations
    /// as a result of censorship.
    /// </summary>
    public HashSet<string> ExcludedLinesByAudioFile { get; set; } = [];
}
