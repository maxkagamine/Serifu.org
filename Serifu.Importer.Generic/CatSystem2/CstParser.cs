using Microsoft.Extensions.Options;
using Serilog;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using TriggersTools.CatSystem2;

namespace Serifu.Importer.Generic.CatSystem2;

internal class CstParser : IParser<CstParserOptions>
{
    private readonly CstParserOptions options;
    private readonly ILogger logger;

    private static readonly JsonSerializerOptions JsonDumpOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public CstParser(IOptions<CstParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<CstParser>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        CstScene scene = CstScene.Extract(path);

        if (options.DumpCst)
        {
            using var jsonFile = File.Create($"{path}.json");
            JsonSerializer.Serialize(jsonFile, scene.Cast<object>(), JsonDumpOptions);
        }

        return [];
    }
}
