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
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using TriggersTools.CatSystem2;
using TriggersTools.CatSystem2.Scenes;
using TriggersTools.CatSystem2.Scenes.Commands.Sounds;

namespace Serifu.Importer.Generic.CatSystem2;

internal sealed class CstParser : IParser<CstParserOptions>
{
    private readonly CstParserOptions options;
    private readonly ILogger logger;

    private static readonly JsonSerializerOptions JsonDumpOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private sealed class CurrentLine
    {
        public List<string> AudioFiles { get; } = [];

        public string? SpeakerName { get; set; }

        public List<string> Texts { get; } = [];

        public override string ToString()
        {
            StringBuilder str = new();
            str.Append($"{nameof(AudioFiles)} = ");
            str.AppendJoin(", ", AudioFiles);
            str.Append(CultureInfo.InvariantCulture, $"; {nameof(SpeakerName)} = {SpeakerName}");
            str.Append($"; {nameof(Texts)} = ");
            str.AppendJoin(", ", Texts);
            return str.ToString();
        }
    }

    public CstParser(IOptions<CstParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<CstParser>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        var logger = this.logger.ForContext("CstFile", path);

        CstScene scene = CstScene.Extract(path);

        if (options.DumpCst)
        {
            using var jsonFile = File.Create($"{path}.json");
            JsonSerializer.Serialize(jsonFile, scene.Cast<object>(), JsonDumpOptions);
        }

        CurrentLine current = new();
        bool hasAudioForLanguage = options.AudioDirectories.ContainsKey(language);

        foreach (ISceneLine? line in scene)
        {
            if (line is SoundPlayCommand { SoundType: SoundType.Pcm } voice)
            {
                current.AudioFiles.Add(voice.Sound);
            }
            else if (line is SceneName name && !string.IsNullOrEmpty(name.Name))
            {
                current.SpeakerName = name.Name.Split('＠' /* U+FF20 */)[0].Replace('_', ' ');
            }
            else if (line is SceneMessage message)
            {
                current.Texts.Add(message.Message);
            }
            else if (line is SceneInput or ScenePage)
            {
                if (current.AudioFiles.Count > 1)
                {
                    logger.Warning("Skipping dialogue with multiple audio files: {@Dialogue}", current);
                }
                else if (current.AudioFiles.Count == 1 && current.Texts.Count > 0 && // Skip unvoiced lines and ignore Inputs not terminating dialogue (no Message)
                         !options.ExcludedLinesByAudioFile.Contains(current.AudioFiles[0]))
                {
                    yield return new ParsedQuoteTranslation()
                    {
                        Key = current.AudioFiles[0],
                        Language = language,
                        Text = string.Join("", current.Texts),
                        AudioFilePath = hasAudioForLanguage ? current.AudioFiles[0] : null,
                        SpeakerName = current.SpeakerName ?? ""
                    };
                }

                // Reset current dialogue
                current = new();
            }
        }

        if (current.Texts.Count > 0)
        {
            logger.Fatal("Reached end of scene with dialogue not terminated by an Input: {@Dialogue}", current);
            throw new Exception($"Reached end of scene with dialogue not terminated by an Input: {current}");
        }
    }
}
