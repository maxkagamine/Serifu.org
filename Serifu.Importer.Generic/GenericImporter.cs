using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Importer.Generic;

internal class GenericImporter
{
    private readonly IParser parser;
    private readonly ParserOptions options;
    private readonly ILogger logger;

    public GenericImporter(
        IParser parser,
        IOptions<ParserOptions> options,
        ILogger logger)
    {
        this.parser = parser;
        this.options = options.Value;
        this.logger = logger.ForContext<GenericImporter>();
    }

    public Task Run(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
