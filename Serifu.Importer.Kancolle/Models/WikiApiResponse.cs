namespace Serifu.Importer.Kancolle.Models;

internal record WikiApiResponse(WikiApiParseResponse? Parse);
internal record WikiApiParseResponse(string? Title, string? Text);
