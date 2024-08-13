// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

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
