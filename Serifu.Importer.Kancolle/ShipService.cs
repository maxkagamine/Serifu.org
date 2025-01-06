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
using Fastenshtein;
using Ganss.Xss;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.Importer.Kancolle.Models;
using Serifu.ML.Abstractions;
using Serilog;
using System.Net;
using static Serifu.Data.Sqlite.ImportHelper;
using static Serifu.Importer.Kancolle.Regexes;

namespace Serifu.Importer.Kancolle;

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

    private readonly WikiClient wiki;
    private readonly ContextTranslator translationService;
    private readonly ISqliteService sqliteService;
    private readonly IWordAligner wordAligner;
    private readonly ILogger logger;
    private readonly HtmlSanitizer htmlSanitizer;

    public ShipService(
        WikiClient wiki,
        ContextTranslator translationService,
        ISqliteService sqliteService,
        IWordAligner wordAligner,
        ILogger logger)
    {
        this.wiki = wiki;
        this.translationService = translationService;
        this.sqliteService = sqliteService;
        this.wordAligner = wordAligner;
        this.logger = logger.ForContext<ShipService>();

        htmlSanitizer = new HtmlSanitizer();
        htmlSanitizer.AllowedTags.Clear();
        htmlSanitizer.AllowedTags.Add("a");
        htmlSanitizer.AllowedTags.Add("br");
        htmlSanitizer.AllowedAttributes.Clear();
        htmlSanitizer.AllowedAttributes.Add("href");
        htmlSanitizer.KeepChildNodes = true;
        htmlSanitizer.PostProcessNode += (object? sender, PostProcessNodeEventArgs e) =>
        {
            if (e.Node is IHtmlAnchorElement a)
            {
                a.Target = "_blank";
                a.Relation = "external noreferrer nofollow";
            }
        };
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
        using var document = await wiki.GetPage(ship.EnglishName, cancellationToken);

        List<Quote> quotes = [];

        foreach (var row in FindQuoteRows(document))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Extract text from page
            List<string> referenceIds = [];

            string scenario = GetText(row.Scenario);
            string textEnglish = FormatEnglishText(GetText(row.English, referenceIds));
            string textJapanese = FormatJapaneseText(GetText(row.Japanese));
            string? audioFileUrl = row.PlayButton?.Href;

            string unsafeNotes = string.Join("<br>\n", (row.Notes is null ? [] : new[] { row.Notes })
                .Concat(GetReferences(document, referenceIds))
                .Where(el => !string.IsNullOrWhiteSpace(el.TextContent))
                .Select(el => el.InnerHtml.Trim()));

            int wordCountEnglish = wordAligner.EnglishTokenizer.GetWordCount(textEnglish);
            int wordCountJapanese = wordAligner.JapaneseTokenizer.GetWordCount(textJapanese);

            // Validate
            //
            // Note: GetQuotesForExport() does the "Japanese text contains kanji or hiragana" check for us so that we
            // can keep the ships' non-Japanese lines saved in the local db for backup purposes, even though we can't
            // use them in the app. Verniy's Russian voice is too cute to throw out, after all.
            if (EmptyOrQuestionMarks.IsMatch(textEnglish))
            {
                logger.Warning("{Ship}'s {Context} quote is missing a translation.", ship, scenario);
                continue;
            }

            if (EmptyOrQuestionMarks.IsMatch(textJapanese))
            {
                logger.Warning("{Ship}'s {Context} quote is missing the original Japanese.", ship, scenario);
                continue;
            }

            if ((double)JapaneseCharacters.Count(textEnglish) / textEnglish.Length > 0.5) // May contain kaomoji
            {
                logger.Warning("{Ship}'s {Context} quote has Japanese on the English side.", ship, scenario);
                continue;
            }

            // Download audio file
            Task<string>? audioFileTask = null;
            if (audioFileUrl is not null)
            {
                try
                {
                    audioFileTask = sqliteService.DownloadAudioFile(audioFileUrl, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
                {
                    logger.Warning("{Ship}'s {Context} audio file {Url} returned {StatusCode}.",
                        ship, scenario, audioFileUrl, (int)ex.StatusCode);
                }
                catch (UnsupportedAudioFormatException ex)
                {
                    logger.Warning("{Ship}'s {Context} audio file {Url} is invalid: {Message}",
                        ship, scenario, audioFileUrl, ex.Message);
                }
            }

            // Run word alignment
            var alignmentDataTask = wordAligner.AlignSymmetric(textEnglish, textJapanese, cancellationToken);

            // Wait for both to complete
            await Task.WhenAll(audioFileTask ?? Task.CompletedTask, alignmentDataTask);

            // Create quote
            (string contextEnglish, string contextJapanese) = translationService.TranslateContext(ship, scenario);
            string sanitizedNotes = htmlSanitizer.Sanitize(unsafeNotes, document.BaseUri).Trim();

            if (ContextInSeasonalQuoteNotes.IsMatch(sanitizedNotes))
            {
                // Seasonal quotes sometimes have "Secretary 1" etc. in the notes since the first column is occupied by
                // the event name; this should eliminate most of those while leaving the more useful translation notes.
                sanitizedNotes = "";
            }

            var quote = new Quote()
            {
                Id = QuoteId.CreateKancolleId(ship.ShipNumber, index: quotes.Count),
                Source = Source.Kancolle,
                English = new()
                {
                    SpeakerName = ship.EnglishName,
                    Context = contextEnglish,
                    Text = textEnglish,
                    WordCount = wordCountEnglish,
                    Notes = sanitizedNotes,
                },
                Japanese = new()
                {
                    SpeakerName = ship.JapaneseName,
                    Context = contextJapanese,
                    Text = textJapanese,
                    WordCount = wordCountJapanese,
                    AudioFile = audioFileTask?.Result,
                },
                AlignmentData = alignmentDataTask.Result.ToArray()
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
            .GroupBy(q => q.Japanese.AudioFile, q => (q.English.Context, q.Japanese.Text))
            .Where(g => g.Key is not null && g.DistinctBy(q => q.Text).Count() > 1))
        {
            logger.Warning("{Ship} has quotes with different Japanese but same audio file: {@Group}.", ship, new
            {
                Quotes = group.Select(q => new { q.Context, q.Text }),
                AudioFile = group.Key,
                Similarity = group.SelectMany(q1 => group.Except([q1]).Select(q2 => (q1, q2)))
                    .Min(x => 1 - (float)Levenshtein.Distance(x.q1.Text, x.q2.Text) / Math.Max(x.q1.Text.Length, x.q2.Text.Length))
            });
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
        foreach (var tr in document.QuerySelectorAll(RowsOfTopLevelTablesSelector))
        {
            var scenario = tr.QuerySelector(ScenarioSelector);
            var playButton = tr.QuerySelector<IHtmlAnchorElement>(PlayButtonSelector);
            var english = tr.QuerySelector(EnglishSelector);
            var japanese = tr.NextElementSibling?.QuerySelector(JapaneseSelector);
            var notes = tr.QuerySelector(NotesSelector);

            if (scenario is not null && english is not null && japanese is not null)
            {
                // Sanity check
                var nearestHeader = tr.Closest("table")!;
                while (nearestHeader is not null && nearestHeader.TagName != "H2")
                {
                    nearestHeader = nearestHeader.PreviousElementSibling;
                }

                if (nearestHeader?.TextContent.Trim() != "Voice Lines")
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
}
