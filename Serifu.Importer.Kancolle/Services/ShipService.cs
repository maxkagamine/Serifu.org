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

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.Importer.Kancolle.Models;
using Serilog;
using System.Net;
using static Serifu.Importer.Kancolle.Helpers.Regexes;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the individual ship pages.
/// </summary>
internal class ShipService
{
    private const string RowsOfTopLevelTablesSelector = ".mw-parser-output > table tr";
    private const string ScenarioSelector = "td[rowspan=2]:first-child b";
    private const string PlayButtonSelector = "td[rowspan=2]:first-child a[title='Play']";
    private const string EnglishSelector = "td:nth-child(2)";
    private const string JapaneseSelector = "td:only-child"; // Second row
    private const string NotesSelector = "td[rowspan=2]:last-child"; // SeasonalQuote only

    private readonly WikiApiService wikiApiService;
    private readonly TranslationService translationService;
    private readonly ISqliteService sqliteService;
    private readonly ILogger logger;
    private readonly HtmlSanitizer htmlSanitizer;

    public ShipService(
        WikiApiService wikiApiService,
        TranslationService translationService,
        ISqliteService sqliteService,
        ILogger logger)
    {
        this.wikiApiService = wikiApiService;
        this.translationService = translationService;
        this.sqliteService = sqliteService;
        this.logger = logger.ForContext<ShipService>();

        htmlSanitizer = new HtmlSanitizer();
        htmlSanitizer.AllowedTags.Clear();
        htmlSanitizer.AllowedTags.Add("a");
        htmlSanitizer.AllowedTags.Add("br");
        htmlSanitizer.AllowedAttributes.Clear();
        htmlSanitizer.AllowedAttributes.Add("href");
        htmlSanitizer.KeepChildNodes = true;
    }

    /// <summary>
    /// Fetches the given ship's wiki page and extracts their list of quotes.
    /// </summary>
    /// <param name="ship">The ship to look up.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of newly-created <see cref="Quote"/> entities (not yet added to the database).</returns>
    public async Task<IEnumerable<Quote>> GetQuotes(Ship ship, CancellationToken cancellationToken = default)
    {
        logger.Information("Fetching wiki page for {Ship}.", ship);
        using var document = await wikiApiService.GetPage(ship.EnglishName, cancellationToken);

        List<Quote> quotes = [];

        foreach (var row in FindQuoteRows(document))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Extract text from page
            List<string> referenceIds = [];

            string context = NormalizeContext(GetText(row.Scenario), ship);
            string textEnglish = GetText(row.English, referenceIds);
            string textJapanese = GetText(row.Japanese);
            string? audioFileUrl = row.PlayButton?.Href;

            string unsafeNotes = string.Join("<br>\n", (row.Notes is null ? [] : new[] { row.Notes })
                .Concat(GetReferences(document, referenceIds))
                .Where(el => !string.IsNullOrWhiteSpace(el.TextContent))
                .Select(el => el.InnerHtml.Trim()));

            // Validate
            if (EmptyOrQuestionMarks.IsMatch(textEnglish))
            {
                logger.Warning("{Ship}'s {Context} quote is missing a translation.", ship, context);
                continue;
            }

            if (EmptyOrQuestionMarks.IsMatch(textJapanese))
            {
                logger.Warning("{Ship}'s {Context} quote is missing the original Japanese.", ship, context);
                continue;
            }

            if (JapaneseCharacters.Count(textEnglish) / textEnglish.Length > 0.5) // May contain kaomoji
            {
                logger.Warning("{Ship}'s {Context} quote has Japanese on the English side.", ship, context);
                continue;
            }

            // Download audio file
            string? audioFile = null;
            if (audioFileUrl is not null)
            {
                try
                {
                    audioFile = await sqliteService.DownloadAudioFile(audioFileUrl, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
                {
                    logger.Warning("{Ship}'s {Context} audio file {Url} returned {StatusCode}.",
                        ship, context, audioFileUrl, (int)ex.StatusCode);
                }
                catch (UnsupportedAudioFormatException ex)
                {
                    logger.Warning("{Ship}'s {Context} audio file {Url} is invalid: {Message}",
                        ship, context, audioFileUrl, ex.Message);
                }
            }

            // Create quote
            var quote = new Quote()
            {
                Id = QuoteId.CreateKancolleId(ship.ShipNumber, index: quotes.Count),
                Source = Source.Kancolle,
                English = new()
                {
                    SpeakerName = ship.EnglishName,
                    Context = context,
                    Text = textEnglish,
                    Notes = htmlSanitizer.Sanitize(unsafeNotes, document.BaseUri).Trim(),
                },
                Japanese = new()
                {
                    SpeakerName = ship.JapaneseName,
                    Context = translationService.TranslateContext(ship, context),
                    Text = textJapanese,
                    AudioFile = audioFile,
                }
            };

            // Check for duplicates (Kasuga Maru has a separate table for her Taiyou remodel with many of the same lines)
            if (quotes.Any(q =>
                q.English.Context == quote.English.Context &&
                q.English.Text == quote.English.Text &&
                q.Japanese.Text == quote.Japanese.Text &&
                q.Japanese.AudioFile == quote.Japanese.AudioFile))
            {
                continue;
            }

            quotes.Add(quote);
        }

        if (quotes.Count == 0)
        {
            logger.Warning("No quotes found for {Ship}.", ship);
        }
        else
        {
            logger.Information("Found {Count} quotes for {Ship}.", quotes.Count, ship);
        }

        // Log warnings for potential mistakes in the wiki
        foreach (var group in quotes
            .GroupBy(q => q.Japanese.AudioFile)
            .Where(g => g.Key is not null && g.DistinctBy(q => q.Japanese.Text).Count() > 1))
        {
            logger.Warning("{Ship} has quotes with different Japanese but same audio file {AudioFile}: {@Quotes}.",
                ship, group.Key, group.Select(q => new { q.English.Context, q.Japanese.Text }));
        }

        return quotes;
    }

    /// <summary>
    /// Finds all ShipquoteKai and SeasonalQuote rows present in the document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>A collection of <see cref="QuoteTableRow"/> containing the relevant elements.</returns>
    private IEnumerable<QuoteTableRow> FindQuoteRows(IDocument document)
    {
        foreach (IElement tr in document.QuerySelectorAll(RowsOfTopLevelTablesSelector))
        {
            var scenario = tr.QuerySelector(ScenarioSelector);
            var playButton = tr.QuerySelector<IHtmlAnchorElement>(PlayButtonSelector);
            var english = tr.QuerySelector(EnglishSelector);
            var japanese = tr.NextElementSibling?.QuerySelector(JapaneseSelector);
            var notes = tr.QuerySelector(NotesSelector);

            if (scenario is not null && english is not null && japanese is not null)
            {
                // Sanity check
                IElement? nearestHeader = tr.Closest("table")!;
                while (nearestHeader is not null && nearestHeader.TagName != "H2")
                {
                    nearestHeader = nearestHeader.PreviousElementSibling;
                }

                if (nearestHeader?.TextContent.Trim() != "Voice Lines[edit]")
                {
                    logger.Warning("Returning a quote that doesn't appear to be within the \"Voice Lines\" section... check {Url} and make sure this is ok. Header = {Header}, Scenario = {Scenario}, English = {English}",
                        document.Url, nearestHeader?.TextContent, scenario.TextContent, english.TextContent);
                }

                yield return new QuoteTableRow(scenario, playButton, english, japanese, notes);
            }
        }
    }

    /// <summary>
    /// Returns the trimmed text content without reference links.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="referenceIds">Collection to which to add the extracted reference ids.</param>
    /// <returns>A plain text string.</returns>
    private static string GetText(IElement element, ICollection<string>? referenceIds = null)
    {
        var clonedElement = (IElement)element.Clone();
        RemoveReferenceLinks(clonedElement, referenceIds);
        return clonedElement.TextContent.Trim();
    }

    /// <summary>
    /// Removes the superscript reference numbers from the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="referenceIds">Collection to which to add the extracted reference ids.</param>
    private static void RemoveReferenceLinks(IElement element, ICollection<string>? referenceIds = null)
    {
        foreach (var reference in element.QuerySelectorAll("sup.reference"))
        {
            if (referenceIds is not null)
            {
                var link = reference.QuerySelector<IHtmlAnchorElement>("a") ?? throw new Exception("Reference number is not linked.");
                var id = new Flurl.Url(link.Href).Fragment;

                referenceIds.Add(id);
            }

            reference.Remove();
        }
    }

    /// <summary>
    /// Gets the elements containing the reference text for each reference id.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="referenceIds">The reference ids collected by <see cref="RemoveReferenceLinks(IElement, ICollection{string}?)"/>.</param>
    /// <returns>The corresponding <c>.reference-text</c> elements.</returns>
    private static IEnumerable<IElement> GetReferences(IDocument document, IEnumerable<string> referenceIds)
        => referenceIds.Select(id => document.GetElementById(id)?.QuerySelector(".reference-text") ?? throw new Exception($"No reference for {id}."));

    /// <summary>
    /// Title-cases the context and fixes some minor inconsistencies.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The normalized context.</returns>
    private static string NormalizeContext(string context, Ship ship)
    {
        // Air Battle/ Daytime Spotting/ Night Battle Attack => Air Battle / Daytime Spotting / Night Battle Attack
        context = Slash.Replace(context, " / ");

        // Valentine’s Day 2017 => Valentine's Day 2017
        context = context.Replace('’', '\'');

        // Minor Damage2 => Minor Damage 2, NightBattle => Night Battle
        context = FirstCharacterOfPascalCaseWord.Replace(context, x => " " + x.ToString());

        // Title case
        context = FirstCharacterOfWord.Replace(context, x => x.ToString().ToUpper());
        context = TitleCaseLowercaseWords.Replace(context, x => x.ToString().ToLower());

        // Equipment 2 => Equipment (excludes numbers in parenthesis in case someone writes Kai 2 instead of Kai Ni)
        context = SingleDigitNumberAfterContext.Replace(context, "");

        // Docking (Major), Docking Major => Docking (Major Damage)
        context = DockingMajorMinorDamage.Replace(context, x => $"Docking ({x.Groups[1]} Damage)");

        // Summer Event 2019 => Summer 2019, Hinamatsuri 2020 Mini-Event => Hinamatsuri 2020, 7th Anniversary 2020 => 7th Anniversary
        context = EventNextToYear.Replace(context, "");
        context = YearNextToAnniversary.Replace(context, "");

        // Special => Special Attack (but not when followed by other words, so no "Special Attack Attack" or "Special Attack Event")
        context = JustSpecial.Replace(context, "Special Attack");

        // Normalize specific patterns, based on most prominent usage in dataset
        context = context
            .Replace("Daytime Spotting / Air Battle / Night Battle Attack",
                     "Air Battle / Daytime Spotting / Night Battle Attack")
            .Replace("Night Attack", "Night Battle Attack")
            .Replace("Secondary Attack", "Night Battle Attack")
            .Replace("Starting Sortie", "Starting a Sortie")
            .Replace("Starting Battle", "Starting a Battle")
            .Replace("Battle Start", "Starting a Battle")
            .Replace("Joining a Fleet", "Joining the Fleet")
            .Replace("Saury Festival", "Saury")
            .Replace("Secretary Married", "Secretary (Married)")
            .Replace("Secretary (Idle)", "Secretary Idle")
            .Replace("Looking at Scores", "Player's Score")
            .Replace("New Years", "New Year")
            .Replace("Return From Sortie", "Returning From Sortie")
            .Replace($"{ship.EnglishName} Special Attack", "Special Attack");
        
        if (context.StartsWith("Idle"))
        {
            context = context.Replace("Idle", "Secretary Idle");
        }
        
        if (context == "Intro")
        {
            context = "Introduction";
        }

        return context;
    }
}
