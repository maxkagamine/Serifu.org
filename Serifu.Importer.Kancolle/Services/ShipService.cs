using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Models;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the individual ship pages.
/// </summary>
internal partial class ShipService
{
    private readonly WikiApiService wikiApiService;
    private readonly ILogger logger;

    [GeneratedRegex(@"\[\[(?:.*?\|)?(.*?)\]\]")]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex(@"^[\s\?]*$")]
    private static partial Regex EmptyOrQuestionMarks();

    public ShipService(
        WikiApiService wikiApiService,
        ILogger logger)
    {
        this.wikiApiService = wikiApiService;
        this.logger = logger.ForContext<ShipService>();
    }

    /// <summary>
    /// Fetches the given ship's wiki page and extracts their list of voice lines.
    /// </summary>
    /// <param name="ship">The ship to look up.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of newly-created <see cref="VoiceLine"/> entities (not yet added to the database).</returns>
    public async Task<IEnumerable<VoiceLine>> GetVoiceLines(Ship ship, CancellationToken cancellationToken = default)
    {
        logger.Information("Fetching wiki page for {Ship}.", ship);
        using var document = await wikiApiService.GetXml(ship.EnglishName, cancellationToken);

        List<VoiceLine> voiceLines = new();
        int i = 0;

        foreach (var template in FindTemplates(document, new[] { "ShipquoteKai", "SeasonalQuote" }))
        {
            // Make sure the template is valid
            if (new[] { "scenario", "translation", "origin" }.Any(x => !template.ContainsKey(x)))
            {
                logger.Warning("One of {Ship}'s voice lines is missing required parameters: {Parameters}.",
                    ship, template.ToDictionary());
                continue;
            }

            if (EmptyOrQuestionMarks().IsMatch(template["translation"]))
            {
                logger.Warning("{Ship}'s {Context} voice line is missing a translation.",
                    ship, FormatContext(template));
                continue;
            }

            if (EmptyOrQuestionMarks().IsMatch(template["origin"]))
            {
                logger.Warning("{Ship}'s {Context} voice line is missing the original Japanese.",
                    ship, FormatContext(template));
                continue;
            }

            // Create voice line entity from template
            var voiceLine = new VoiceLine()
            {
                Source = Source.Kancolle,
                Context = FormatContext(template),
                SpeakerEnglish = ship.EnglishName,
                SpeakerJapanese = ship.JapaneseName,
                TextEnglish = template["translation"],
                TextJapanese = template["origin"],
                Notes = ExtractNotes(template),
                AudioFile = GetAudioFile(ship.EnglishName, template),
                SortOrder = i++,
            };

            if (voiceLine.AudioFile.Contains('['))
            {
                // Brackets indicate that there was no `audio`, so the filename was generated dynamically, but the
                // `scenario` has a wiki link in it. Since template params are expanded verbatim, this will result in an
                // invalid filename in the wiki as well, but we can avoid a 400 by nulling the AudioFile here.
                logger.Warning("{Ship}'s {Context} is missing an audio file, and the auto-generated filename contains invalid characters. Setting to null.",
                    ship, voiceLine.Context);

                voiceLine.AudioFile = null;
            }

            // Check for duplicates (Kasuga Maru has a separate table for her Taiyou remodel w/ many of the same lines)
            if (voiceLines.Any(v =>
                v.Context == voiceLine.Context &&
                v.TextEnglish == voiceLine.TextEnglish &&
                v.TextJapanese == voiceLine.TextJapanese &&
                v.AudioFile == voiceLine.AudioFile))
            {
                continue;

                /* Regarding duplicates, here's my truth table for different cases after analyzing the data (x = same):
                 *
                 * Ctx Jpn Eng Aud
                 *  x   x   x   x   Duplicate (Kasuga Maru only)
                 *  ?   x   x       Sometimes the same line is spoken with different intonation. (There are also some
                 *                  identical audio files; possibly others that are the same too but not byte-for-byte.)
                 *  ?   x       x   Different translations for the same voice line. Only 9 such cases; may as well keep.
                 *  ?   x           Combination of the above two cases.
                 *  ?       ?   x   ⚠️ Could be different punctuation, kanji vs. kana, etc., but could also be a mistake
                 *                  in the wiki (copy-paste error). Log a warning.
                 *  ?       x       Different voice lines that coincidentally have the same translation.
                 *  x               This happens a lot, either due to seasonal quotes putting the specific context in
                 *                  the notes or because the line is for a remodel which was noted elsewhere.
                 *      x   x   x   Same voice line used in different contexts or reused for events. Surprisingly only
                 *                  four, but could be more if we identify copies of the same audio. Could concat the
                 *                  contexts, but since there's so few we may as well leave them as-is.
                 */
            }

            voiceLines.Add(voiceLine);
        }

        if (voiceLines.Count == 0)
        {
            logger.Warning("No voice lines found for {Ship}. Check whether the scraping code is broken or the page actually has no voice lines.", ship);
        }
        else
        {
            logger.Information("Found {Count} voice lines for {Ship}.", voiceLines.Count, ship);
        }

        // Log warnings for potential mistakes in the wiki
        foreach (var group in voiceLines
            .GroupBy(v => v.AudioFile)
            .Where(g => g.Key is not null && g.DistinctBy(v => v.TextJapanese).Count() > 1))
        {
            logger.Warning("{Ship}'s voice lines {Contexts} have different Japanese but the same audio file {AudioFile}: {TextJapaneses}.",
                ship, group.Select(v => v.Context), group.Key, group.Select(v => v.TextJapanese));
        }

        return voiceLines;
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
        if (TryGetRemodelName(template, out var kaiName) && !context.Contains(kaiName))
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
        if (template.TryGetString("audio", out var audio) && !string.IsNullOrEmpty(audio))
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
