
using Ganss.Xss;
using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;

namespace Serifu.Importer.Generic.Larian;

using GameObject = (LsjTranslatedString? DisplayName, Guid? TemplateId, string? Name);

internal class LsjParser : IParser<LsjParserOptions>
{
    private const string NarratorKey = "NARRATOR";
    private const string NarratorStringId = "h0fd7e77ag106bg47d5ga587g6cfeed742d5d";
    private const string TavNamePrefix = "S_Player_GenericOrigin";
    private const string TavStringId = "ha0b302dag3025g44e2gaf63g7fd9ec937241";

    private readonly LsjParserOptions options;
    private readonly ILogger logger;
    private readonly IHostApplicationLifetime lifetime;
    private readonly HtmlSanitizer htmlSanitizer;

    private readonly Dictionary<LsjTranslatedString, string> englishStrings = [];
    private readonly Dictionary<LsjTranslatedString, string> japaneseStrings = [];
    private readonly ConcurrentDictionary<Guid, GameObject> objects = [];

    public LsjParser(IOptions<LsjParserOptions> options, ILogger logger, IHostApplicationLifetime lifetime)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<LsjParser>();
        this.lifetime = lifetime;

        htmlSanitizer = new HtmlSanitizer(new HtmlSanitizerOptions()) { KeepChildNodes = true };

        // The BG3 import operates in two steps: first, the localization files and game objects are indexed (the latter
        // of which involves iterating over all lsj files), then, once indexing is complete, the generic importer begins
        // its process of iterating over the dialogue files, which in this case should be the voice meta files. Note
        // that the BaseDirectory must be the root of the extracted paks (containing Localization, Mods, and Public).
        Initialize().GetAwaiter().GetResult();
    }

    private async Task Initialize()
    {
        using var progress = new TerminalProgressBar();
        progress.SetIndeterminate();

        using (logger.BeginTimedOperation("Indexing localization files"))
        {
            ReadLocalizationXml("Localization/English/english.xml", englishStrings);
            ReadLocalizationXml("Localization/Japanese/japanese.xml", japaneseStrings);
        }

        using (logger.BeginTimedOperation("Indexing game objects"))
        {
            await Parallel.ForEachAsync(
                Directory.EnumerateFiles(options.BaseDirectory, "*.lsj", SearchOption.AllDirectories),
                lifetime.ApplicationStopping,
                ReadGameObjects);
        }
    }

    private void ReadLocalizationXml(string relativePath, Dictionary<LsjTranslatedString, string> strings)
    {
        logger.Information("Reading {File}", relativePath);

        XmlSerializer serializer = new(typeof(LocalizationXml));
        string path = Path.GetFullPath(relativePath, options.BaseDirectory);
        using FileStream file = File.OpenRead(path);
        var xml = (LocalizationXml)serializer.Deserialize(file)!;

        foreach (var content in xml.Content)
        {
            strings.Add(new(content.ContentUid), content.Value);
        }
    }

    private async ValueTask ReadGameObjects(string path, CancellationToken cancellationToken)
    {
        try
        {
            logger.Information("Reading {File}", Path.GetRelativePath(options.BaseDirectory, path).Replace('\\', '/'));

            using FileStream file = File.OpenRead(path);
            LsjFile lsj = (await JsonSerializer.DeserializeAsync(file, LsjSourceGenerationContext.Default.LsjFile, cancellationToken))!;

            if (lsj.Save.Regions.Templates is LsjTemplates templates)
            {
                foreach (LsjGameObject gameObject in templates.GameObjects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (gameObject.MapKey?.Value is not Guid mapKey)
                    {
                        continue;
                    }

                    LsjTranslatedString? displayName = gameObject.DisplayName;
                    Guid? templateId = (gameObject.TemplateName ?? gameObject.ParentTemplateId)?.Value;
                    string? name = gameObject.Name?.Value;

                    if (displayName is null && templateId is null)
                    {
                        continue;
                    }

                    AddOrUpdateGameObject(mapKey, (displayName, templateId, name));
                }
            }

            if (lsj.Save.Regions.Origins is LsjOrigins origins)
            {
                foreach (LsjOrigin origin in origins.Origin)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (origin.GlobalTemplate?.Value is not Guid globalTemplate ||
                        origin.DisplayName is not LsjTranslatedString displayName)
                    {
                        continue;
                    }

                    AddOrUpdateGameObject(globalTemplate, (displayName, null, null));
                }
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to parse {LsjFile}", path);
            throw;
        }
    }

    private void AddOrUpdateGameObject(Guid id, GameObject obj) => objects.AddOrUpdate(
        id,
        (_, current) => current,
        (_, existing, current) => existing == current ? existing : existing with
        {
            DisplayName = existing.DisplayName ?? current.DisplayName,
            TemplateId = existing.TemplateId ?? current.TemplateId,
            Name = existing.Name ?? current.Name
        },
        obj);

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        if (language is not Language.Multilingual)
        {
            throw new ArgumentException($"Language must be {nameof(Language.Multilingual)}.", nameof(language));
        }

        using FileStream file = File.OpenRead(path);
        LsjFile lsj = JsonSerializer.Deserialize(file, LsjSourceGenerationContext.Default.LsjFile)!;

        if (lsj.Save.Regions.VoiceMetaData is not LsjVoiceMetaData voiceMetaData)
        {
            throw new InvalidDataException($"File does not contain a VoiceMetaData region (wrong LSJ type): {path}");
        }

        foreach (LsjVoiceSpeakerMetaData speakerMetaData in voiceMetaData.VoiceSpeakerMetaData)
        {
            LsjTranslatedString? speakerName;
            string speakerIdStr = speakerMetaData.MapKey.Value;
            int weight = 1;
            
            if (speakerIdStr == NarratorKey)
            {
                speakerName = new(NarratorStringId);
            }
            else
            {
                Guid speakerId = Guid.Parse(speakerIdStr);

                if (options.PreferredSpeakerIds.Contains(speakerId))
                {
                    weight = 2;
                }

                speakerName = ResolveSpeakerName(speakerId);
            }

            TryGetString(speakerName, Language.English, out string englishSpeakerName);
            TryGetString(speakerName, Language.Japanese, out string japaneseSpeakerName);

            foreach (LsjVoiceTextMetaData voiceTextMetaData in speakerMetaData.MapValue.SelectMany(x => x.VoiceTextMetaData ?? []))
            {
                LsjTranslatedString stringId = new(voiceTextMetaData.MapKey.Value);
                string audioFile = voiceTextMetaData.MapValue.Single().Source.Value.Replace(".wem", ".opus");

                if (!audioFile.StartsWith($"v{speakerIdStr.Replace("-", "")}"))
                {
                    throw new Exception("Unexpected speaker ID / audio file name mismatch.");
                }

                // Filter out unused/untranslated dialogue
                if (!TryGetString(stringId, Language.English, out string englishText))
                {
                    logger.Warning("Skipping {Key}: {Reason}", audioFile, "String not found in English localization file");
                    continue;
                }

                if (!TryGetString(stringId, Language.Japanese, out string japaneseText))
                {
                    logger.Warning("Skipping {Key}: {Reason}", audioFile, "String found in English but not Japanese localization file");
                    continue;
                }

                if (!File.Exists(Path.Combine(options.BaseDirectory, options.AudioDirectories[Language.English], audioFile)))
                {
                    logger.Warning("Skipping {Key}: {Reason}", audioFile, "Audio file does not exist");
                    continue;
                }

                // Filter out dialogue text containing string interpolation
                if (englishText.Contains('[') || japaneseText.Contains('['))
                {
                    logger.Warning("Skipping {Key}: {Reason}", audioFile, "Dialogue text contains string interpolation");
                    continue;
                }

                yield return new ParsedQuoteTranslation()
                {
                    Key = audioFile,
                    Language = Language.English,
                    Text = englishText,
                    SpeakerName = englishSpeakerName,
                    AudioFilePath = audioFile,
                    Weight = weight
                };

                yield return new ParsedQuoteTranslation()
                {
                    Key = audioFile,
                    Language = Language.Japanese,
                    Text = japaneseText,
                    SpeakerName = japaneseSpeakerName,
                    AudioFilePath = null,
                    Weight = weight
                };
            }
        }
    }

    private LsjTranslatedString? ResolveSpeakerName(Guid? speakerId)
    {
        while (speakerId.HasValue)
        {
            if (!objects.TryGetValue(speakerId.Value, out GameObject gameObject))
            {
                break;
            }

            if (gameObject.Name?.StartsWith(TavNamePrefix) == true)
            {
                return new(TavStringId);
            }

            if (gameObject.DisplayName is LsjTranslatedString displayName)
            {
                return displayName;
            }

            speakerId = gameObject.TemplateId;
        }

        return null;
    }

    private bool TryGetString(LsjTranslatedString? stringId, Language language, out string sanitizedString)
    {
        Dictionary<LsjTranslatedString, string> strings = language switch
        {
            Language.English => englishStrings,
            Language.Japanese => japaneseStrings,
            _ => throw new UnreachableException()
        };

        if (!stringId.HasValue || !strings.TryGetValue(stringId.Value, out string? str))
        {
            sanitizedString = "";
            return false;
        }

        if (language is Language.English)
        {
            str = str.Replace("<br>", " ");
        }

        str = htmlSanitizer.Sanitize(str);
        str = str.Trim(' ', '*', '(', ')');

        sanitizedString = str;
        return true;
    }
}
