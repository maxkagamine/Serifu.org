namespace Serifu.Importer.Generic;

internal interface IParser
{
    /// <summary>
    /// Parses a file and returns the dialogue lines as a collection of <see cref="ParsedQuoteTranslation"/>.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="language">Language hint.</param>
    IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language);
}

internal interface IParser<TOptions> : IParser where TOptions : ParserOptions;
