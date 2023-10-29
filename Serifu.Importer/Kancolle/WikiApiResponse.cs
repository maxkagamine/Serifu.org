using System.Text.Json.Serialization;

namespace Serifu.Importer.Kancolle;

internal class WikiApiResponse
{
    public WikiApiParseResponse? Parse { get; set; }
}

internal class WikiApiParseResponse
{
    public string? Title { get; set; }

    [JsonPropertyName("parsetree")]
    public string? ParseTree { get; set; }
}