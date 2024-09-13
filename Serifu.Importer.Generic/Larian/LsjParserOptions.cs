namespace Serifu.Importer.Generic.Larian;

internal class LsjParserOptions : ParserOptions
{
    public HashSet<Guid> PreferredSpeakerIds { get; set; } = [];
}
