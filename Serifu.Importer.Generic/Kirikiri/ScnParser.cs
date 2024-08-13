using Kagamine.Extensions.Collections;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

namespace Serifu.Importer.Generic.Kirikiri;

internal class ScnParser : IParser<ScnParserOptions>
{
    private readonly ScnParserOptions options;
    private readonly ILogger logger;
    private readonly JsonSerializerOptions jsonOptions;

    public ScnParser(IOptions<ScnParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<ScnParser>();

        jsonOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonArrayToRecordConverter<ScnSceneText>(),
                new JsonArrayToRecordConverter<ScnSceneTextTranslation>(),
                new JsonScnTranslationsConverter(options),
                new JsonValueArrayConverter()
            }
        };
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        if (language is not Language.Multilingual)
        {
            throw new ArgumentException($"Language must be {nameof(Language.Multilingual)}, as {nameof(ScnParser)} expects the .scn file to contain all translations.", nameof(language));
        }

        using FileStream stream = File.OpenRead(path);
        ScnFile scn = JsonSerializer.Deserialize<ScnFile>(stream, jsonOptions)!;

        throw new NotImplementedException();
    }
}
