using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Importer.Generic.Tsv;

internal class TsvParser : IParser<TsvParserOptions>
{
    private readonly TsvParserOptions options;
    private readonly ILogger logger;

    public TsvParser(IOptions<TsvParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<TsvParser>();
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(Stream stream, Language language)
    {
        throw new NotImplementedException();
    }
}
