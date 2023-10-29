using System.Text.Json.Serialization;

namespace Serifu.Importer.Kancolle.Models;

internal partial class WikiApiResponse
{
    public WikiApiParseResponse? Parse { get; set; }
}

internal class WikiApiParseResponse
{
    public string? Title { get; set; }

    [JsonPropertyName("parsetree")]
    public string? ParseTree { get; set; }

    public string? Text { get; set; }
}