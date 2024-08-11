namespace Serifu.Importer.Generic;

internal interface IParser
{
    /// <summary>
    /// Parses a file and returns the dialogue lines as a collection of <see cref="ParsedQuoteTranslation"/>.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="language">Language hint.</param>
    IEnumerable<ParsedQuoteTranslation> Parse(Stream stream, Language language);
}

internal interface IParser<TOptions> : IParser where TOptions : ParserOptions;
