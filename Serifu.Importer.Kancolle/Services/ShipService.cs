using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Serifu.Data;
using Serifu.Data.Entities;
using Serifu.Importer.Kancolle.Models;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the individual ship pages.
/// </summary>
internal partial class ShipService
{
    private const string RowsOfTopLevelTablesSelector = ".mw-parser-output > table tr";
    private const string ScenarioSelector = "td[rowspan=2]:first-child b";
    private const string PlayButtonSelector = "td[rowspan=2]:first-child a[title='Play']";
    private const string EnglishSelector = "td:nth-child(2)";
    private const string JapaneseSelector = "td:only-child"; // Second row
    private const string NotesSelector = "td[rowspan=2]:last-child"; // SeasonalQuote only

    private readonly WikiApiService wikiApiService;
    private readonly ILogger logger;
    private readonly HtmlSanitizer htmlSanitizer;

    public ShipService(WikiApiService wikiApiService, ILogger logger)
    {
        this.wikiApiService = wikiApiService;
        this.logger = logger.ForContext<ShipService>();

        htmlSanitizer = new HtmlSanitizer();
        htmlSanitizer.AllowedTags.Clear();
        htmlSanitizer.AllowedTags.Add("a");
        htmlSanitizer.AllowedTags.Add("br");
        htmlSanitizer.AllowedAttributes.Clear();
        htmlSanitizer.AllowedAttributes.Add("href");
        htmlSanitizer.KeepChildNodes = true;
    }

    [GeneratedRegex(@"^[\s\?]*$")]
    private static partial Regex EmptyOrQuestionMarks();

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
            List<string> referenceIds = [];

            string context = GetText(row.Scenario);
            string textEnglish = GetText(row.English, referenceIds);
            string textJapanese = GetText(row.Japanese);
            string? audioFile = GetFilename(row.PlayButton);

            if (EmptyOrQuestionMarks().IsMatch(textEnglish))
            {
                logger.Warning("{Ship}'s {Context} quote is missing a translation.", ship, context);
                continue;
            }

            if (EmptyOrQuestionMarks().IsMatch(textJapanese))
            {
                logger.Warning("{Ship}'s {Context} quote is missing the original Japanese.", ship, context);
                continue;
            }

            string unsafeNotes = string.Join("<br>\n", (row.Notes is null ? [] : new[] { row.Notes })
                .Concat(GetReferences(document, referenceIds))
                .Where(el => !string.IsNullOrWhiteSpace(el.TextContent))
                .Select(el => el.InnerHtml.Trim()));

            var quote = new Quote()
            {
                Id = QuoteId.CreateKancolleId(ship.ShipNumber, index: quotes.Count),
                Source = Source.Kancolle,
                Translations = [
                    new()
                    {
                        Language = "en",
                        SpeakerName = ship.EnglishName,
                        Context = context,
                        Text = textEnglish,
                        Notes = htmlSanitizer.Sanitize(unsafeNotes, document.BaseUri).Trim(),
                    },
                    new()
                    {
                        Language = "ja",
                        SpeakerName = ship.JapaneseName,
                        Context = context, // TODO: Find Japanese names for contexts
                        Text = textJapanese,
                        AudioFile = audioFile is null ? null : new AudioFile("", "", audioFile, DateTime.Now) // TODO: Hash files
                    }
                ]
            };

            // Check for duplicates (Kasuga Maru has a separate table for her Taiyou remodel with many of the same lines)
            if (quotes.Any(q =>
                q.Translations["en"].Context == quote.Translations["en"].Context &&
                q.Translations["en"].Text == quote.Translations["en"].Text &&
                q.Translations["ja"].Text == quote.Translations["ja"].Text &&
                q.Translations["ja"].AudioFile?.OriginalName == quote.Translations["ja"].AudioFile?.OriginalName))
            {
                continue;
            }

            quotes.Add(quote);
        }

        if (quotes.Count == 0)
        {
            logger.Warning("No quotes found for {Ship}. Check whether the scraping code is broken or the page actually has no quotes.", ship);
        }
        else
        {
            logger.Information("Found {Count} quotes for {Ship}.", quotes.Count, ship);
        }

        // Log warnings for potential mistakes in the wiki
        foreach (var group in quotes
            .GroupBy(q => q.Translations["ja"].AudioFile?.OriginalName)
            .Where(g => g.Key is not null && g.DistinctBy(q => q.Translations["ja"].Text).Count() > 1))
        {
            logger.Warning("{Ship} has quotes with different Japanese but same audio file {AudioFile}: {@Quotes}.",
                ship, group.Key, group.Select(q => new { q.Translations["en"].Context, q.Translations["ja"].Text }));
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
    /// Gets the name of the file the element links to, url decoded.
    /// </summary>
    /// <param name="element">The link element.</param>
    private static string? GetFilename(IHtmlAnchorElement? element)
        => HttpUtility.UrlDecode(element?.Href.Split('/').Last());
}
