
using Kagamine.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Serialization;

namespace Serifu.Importer.Generic.Larian;

using GameObject = (LsjTranslatedString? DisplayName, Guid? TemplateId, string? Name);

internal class LsjParser : IParser<LsjParserOptions>
{
    private readonly LsjParserOptions options;
    private readonly ILogger logger;
    private readonly IHostApplicationLifetime lifetime;

    private readonly Dictionary<LsjTranslatedString, string> englishStrings = [];
    private readonly Dictionary<LsjTranslatedString, string> japaneseStrings = [];
    private readonly ConcurrentDictionary<Guid, GameObject> objects = [];

    public LsjParser(IOptions<LsjParserOptions> options, ILogger logger, IHostApplicationLifetime lifetime)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<LsjParser>();
        this.lifetime = lifetime;

        // The BG3 import operates in two steps: first, the localization files and game objects are indexed (the latter
        // of which involves iterating over all lsj files), then, once indexing is complete, the generic importer begins
        // its process of iterating over the dialogue files, which in this case should be the voice meta files. Note
        // that the BaseDirectory must be the root of the extracted paks (containing Localization, Mods, and Public).
        Initialize().GetAwaiter().GetResult();
    }

    private async Task Initialize()
    {
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
        using var file = File.OpenRead(path);
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
            using var file = File.OpenRead(path);
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
        throw new NotImplementedException();
    }
}
