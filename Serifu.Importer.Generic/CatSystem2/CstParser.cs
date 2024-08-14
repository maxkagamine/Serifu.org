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
