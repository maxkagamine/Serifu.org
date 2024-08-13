
using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Importer.Generic.Kirikiri;

internal class ScnParser : IParser<ScnParserOptions>
{
    private readonly ScnParserOptions options;
    private readonly ILogger logger;

    public ScnParser(IOptions<ScnParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<ScnParser>();
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        throw new NotImplementedException();
    }
}
