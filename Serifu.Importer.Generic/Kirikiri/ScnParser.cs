﻿using Kagamine.Extensions.Collections;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Generic.Kirikiri;

internal partial class ScnParser : IParser<ScnParserOptions>
{
    private readonly ScnParserOptions options;
    private readonly ILogger logger;
    private readonly JsonSerializerOptions jsonOptions;

    [GeneratedRegex("""
        (?<!\\) # Not preceeded by a backslash escape
        (
            # Percent formatting (%l takes an extra argument)
            %(l[^;]*;)?[^;]*; |
            # Furigana
            \[[^\]]*\]
        )
        """,
        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase)]
    private partial Regex FormattingRegex();

    // We'll need to check any new %-tags to see if they take multiple arguments like %l or not.
    [GeneratedRegex(@"(?<!\\)%[^\d;dfl]", RegexOptions.IgnoreCase)]
    private partial Regex UnknownPercentTagRegex();

    // The ampersand was seen escaped, which implies it might have special meaning (perhaps HTML entities are allowed?)
    [GeneratedRegex(@"(?<!\\)&")]
    private partial Regex UnescapedAmpersand();

    [GeneratedRegex(@"\\.", RegexOptions.Singleline)]
    private partial Regex EscapeCharacter();

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
        string filename = Path.GetFileName(path);
        ScnFile file = JsonSerializer.Deserialize<ScnFile>(stream, jsonOptions)!;

        // These will basically always be false & true respectively, but this should work even if the voice isn't jp
        bool hasEnglishAudio = options.AudioDirectories.ContainsKey(Language.English);
        bool hasJapaneseAudio = options.AudioDirectories.ContainsKey(Language.Japanese);

        foreach (ScnScene scene in file.Scenes)
        {
            string englishContext = MapSceneTitle(scene.Title.English, Language.English);
            string japaneseContext = MapSceneTitle(scene.Title.Japanese, Language.Japanese);

            int i = 0;

            foreach (ScnSceneText sceneText in scene.Texts)
            {
                string key = $"{filename}{scene.Label}[{i++}]";

                // Skip lines where multiple people are talking at the same time (usu. shouting in unison), as the text
                // often contains multiple quotes in one, and they aren't particularly useful either.
                if (sceneText.VoiceFiles.Length > 1)
                {
                    logger.Warning("Skipping {Key}: {Reason}", key, "Multiple people talking at the same time");
                    continue;
                }

                yield return new ParsedQuoteTranslation()
                {
                    Key = key,
                    Language = Language.English,
                    Text = StripFormatting(sceneText.Translations.English.FormattedText, key),
                    AudioFilePath = hasEnglishAudio ? sceneText.VoiceFiles.SingleOrDefault()?.Voice : null,
                    SpeakerName = sceneText.SpeakerName ?? "", // Ignoring the display name & using a configured map for translation instead, to avoid names like "???" or "Box" (when it's Chocola in the box)
                    Context = englishContext
                };

                yield return new ParsedQuoteTranslation()
                {
                    Key = key,
                    Language = Language.Japanese,
                    Text = StripFormatting(sceneText.Translations.Japanese.FormattedText, key),
                    AudioFilePath = hasJapaneseAudio ? sceneText.VoiceFiles.SingleOrDefault()?.Voice : null,
                    SpeakerName = sceneText.SpeakerName ?? "", // Likewise
                    Context = japaneseContext
                };
            }
        }
    }

    private string StripFormatting(string text, string key)
    {
        // Check for unknown percent tags
        Match match = UnknownPercentTagRegex().Match(text);
        if (match.Success)
        {
            throw new Exception($"""
                {key} contains an unknown percent formatting "{match.Value}".
                Check how many arguments it takes (how many semicolons follow it) and update the regexes.
                """);
        }

        // Remove percent tags & furigana
        text = FormattingRegex().Replace(text, "");

        // Check for unescaped ampersands
        match = UnescapedAmpersand().Match(text);
        if (match.Success)
        {
            throw new Exception($"{key} contains an unescaped ampersand; check if these have special meaning or not.");
        }

        // Unescape text
        text = EscapeCharacter().Replace(text, m => m.ValueSpan[1] switch
        {
            '\\' => "\\",
            '%' => "%",
            '&' => "&",
            'n' => "\n",
            _ => throw new Exception($"""
                {key} contains an unknown character escape "{m.Value}".
                Check if it's a literal character / standard C escape or if it has different meaning.
                """)
        });

        return text;
    }

    private string MapSceneTitle(string title, Language language)
    {
        if (!options.UseSceneTitleAsContext)
        {
            return "";
        }

        if (options.SceneTitleMap.TryGetValue(language, out var map) && map.TryGetValue(title, out var mappedTitle))
        {
            return mappedTitle;
        }

        if (language is Language.English && title != "")
        {
            // Convert full-width spaces to half-width and capitalize first letter (fixes Senren Banka's titles)
            title = title.Replace('　', ' ');
            title = string.Concat(title[0].ToString().ToUpperInvariant(), title.AsSpan(1));
        }

        return title.Trim();
    }
}
