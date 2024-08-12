namespace Serifu.Importer.Generic.Kirikiri;

internal class KsParserOptions : ParserOptions
{
    // The config binder appends to arrays, and there doesn't seem to be a way to make it replace the default instead.
    public static readonly string[] DefaultLineSeparatorTags = ["l", "p"];

    /// <summary>
    /// These tags are used to separate lines of dialogue.
    /// </summary>
    public string[]? LineSeparatorTags { get; set; }

    /// <summary>
    /// If any of these tags are present in the dialogue line, the quote will stop there and the rest will be discarded.
    /// This is used to strip off ", said Tsubaki" etc., since that extra bit seems a little weird in the context where
    /// we'd be displaying the quotes.
    /// </summary>
    public string[] QuoteStopTags { get; set; } = [];
}
