using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Models;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the individual ship pages.
/// </summary>
internal partial class ShipService
{
    private readonly WikiApiService wikiApiService;
    private readonly ILogger<ShipService> logger;

    [GeneratedRegex(@"\[\[(?:.*?\|)?(.*?)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikiLinkRegex();

    public ShipService(
        WikiApiService wikiApiService,
        ILogger<ShipService> logger)
    {
        this.wikiApiService = wikiApiService;
        this.logger = logger;
    }

    /// <summary>
    /// Fetches the given ship's wiki page and extracts their list of quotes.
    /// </summary>
    /// <param name="ship">The ship to look up.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of newly-created <see cref="Quote"/> entities (not yet added to the database).</returns>
    public async Task<IEnumerable<Quote>> GetQuotes(Ship ship, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching wiki page for {Ship}.", ship);
        using var document = await wikiApiService.GetXml(ship.EnglishName, cancellationToken);

        List<Quote> quotes = new();

        foreach (var template in FindTemplates(document, new[] { "ShipquoteKai", "SeasonalQuote" }))
        {
            var missingParameters = new[] { "scenario", "translation", "origin" }
                .Where(x => !template.ContainsKey(x)).ToArray();
            if (missingParameters.Any())
            {
                logger.LogWarning("One of {Ship}'s quotes is missing required parameters: {MissingParameters}.",
                    ship, missingParameters);

                continue;
            }

            var quote = new Quote()
            {
                Source = Source.Kancolle,
                Context = FormatContext(template),
                SpeakerEnglish = ship.EnglishName,
                SpeakerJapanese = ship.JapaneseName,
                QuoteEnglish = template["translation"],
                QuoteJapanese = template["origin"],
                Notes = ExtractNotes(template),
                AudioFile = GetAudioFile(ship.EnglishName, template),
            };

            quotes.Add(quote);
        }

        if (quotes.Count == 0)
        {
            logger.LogWarning("""
                No quotes found for {Ship}.
                Check whether the scraping code is broken or the page actually has no quotes.
                """, ship);
        }
        else
        {
            logger.LogInformation("Found {Count} quotes for {Ship}.", quotes.Count, ship);
        }

        return quotes;
    }

    private static IEnumerable<WikiTemplate> FindTemplates(IDocument document, string[] templateNames)
        => document.GetElementsByTagName("template")
            .Select(el => new WikiTemplate(el))
            .Where(t => templateNames.Any(name => name.Equals(t.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private static string ExtractNotes(WikiTemplate template)
    {
        List<string> allNotes = new();

        var refs = template.GetXml("translation")
            .QuerySelectorAll("ext")
            .Where(ext => ext.GetChild("name").TextContent == "ref")
            .Select(ext => ext.GetChild("inner").GetTextNodes());

        if (template.TryGetString("notes", out var seasonalQuoteNotes))
        {
            allNotes.Add(seasonalQuoteNotes);
        }

        allNotes.AddRange(refs);
        return string.Join('\n', allNotes);
    }

    private static string FormatContext(WikiTemplate template)
    {
        var context = template["scenario"];

        // ShipquoteKai template does this (for hourlies)
        if (context.Length == 2)
        {
            context += ":00";
        }

        // Seasonal quotes often link to the event page
        context = WikiLinkRegex().Replace(context, "$1");

        // Attempt to differentiate remodel-specific lines. Getting the proper remodel names for all ships would require
        // a bunch of special cases, but this should work for most ships.
        if (TryGetRemodelName(template, out var kaiName))
        {
            context += $" ({kaiName})";
        }

        return context;
    }

    /// <summary>
    /// Returns <c>audio</c> if present, else tries to piece together the filename in the same manner as the
    /// ShipquoteKai template.
    ///
    /// This and <see cref="TryGetRemodelName(Dictionary{string, string})"/> needs to be kept in sync with
    /// https://en.kancollewiki.net/w/index.php?title=Template:ShipquoteKai&action=edit.
    ///
    /// Last revision: August 29th, 2023
    ///
    /// (It might be better to rewrite all of this to scrape the html instead of the xml parse tree. Which is going to
    /// be least fragile is honestly a toss-up, and both have disadvantages.)
    /// </summary>
    private static string GetAudioFile(string pageName, WikiTemplate template)
    {
        if (template.TryGetString("audio", out var audio))
        {
            return audio;
        }

        StringBuilder url = new("Ship Voice ");

        url.Append(template.GetStringOrDefault("name") ?? pageName);

        if (TryGetRemodelName(template, out var kaiName))
        {
            url.AppendFormat(" {0}", kaiName);
        }

        url.Append(' ');

        if (template.TryGetString("line", out var line))
        {
            url.Append(line);
        }
        else if (template["scenario"].Length > 2 && template["scenario"][2] == ':')
        {
            url.Append(template["scenario"][..2]);
        }
        else if (template["scenario"] == "Air Battle/Daytime Spotting/Night Battle Attack")
        {
            url.Append("Night Attack");
        }
        else
        {
            url.Append(template["scenario"]);
        }

        return url.Append(".mp3").ToString();
    }

    private static bool TryGetRemodelName(WikiTemplate template, [NotNullWhen(true)] out string? kaiName)
    {
        kaiName =
            template.ContainsKey("kai") ? "Kai" :
            template.ContainsKey("kai2") ? "Kai Ni" :
            template.ContainsKey("kai3") ? "Kai San" :
            template.TryGetString("form2", out var form2) ? form2 :
            template.ContainsKey("kai2b") ? "Kai Ni B" :
            template.ContainsKey("kaic") ? "Kai Ni C" :
            template.ContainsKey("kai2c") ? "Kai Ni C" :
            template.ContainsKey("kai2d") ? "Kai Ni D" :
            template.ContainsKey("kai2e") ? "Kai Ni E" :
            template.ContainsKey("kai2toku") ? "Kai Ni Toku" :
            template.ContainsKey("kaigo") ? "Kai Ni Go" :
            template.ContainsKey("kai2j") ? "Kai Ni Juu" :
            null;

        return kaiName is not null;
    }
}
