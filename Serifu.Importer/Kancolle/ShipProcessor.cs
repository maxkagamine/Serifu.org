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

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using AngleSharp.Dom;
using AngleSharp.Xml.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serifu.Data;
using Serifu.Data.Entities;
using Serifu.Importer.Helpers;
using Url = Flurl.Url;

namespace Serifu.Importer.Kancolle;

/// <summary>
/// Processes each ship placed on the queue by <see cref="ShipListProcessor"/> in turn, fetching their quotes from the
/// wiki and adding them to the database while also adding the corresponding audio files to the queue to be downloaded
/// by <see cref="AudioFileProcessor"/> in the background.
/// </summary>
internal partial class ShipProcessor : IProcessor
{
    const string WikiApiUrl = "https://en.kancollewiki.net/w/api.php";

    [GeneratedRegex(@"\[\[(?:.*?\|)?(.*?)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikiLinkRegex();

    private readonly Channel<ShipQueueItem> shipQueue;
    private readonly Channel<AudioFileQueueItem> audioFileQueue;
    private readonly QuotesContext db;
    private readonly HttpClient httpClient;
    private readonly ILogger<ShipProcessor> logger;

    public ShipProcessor(
        Channel<ShipQueueItem> shipQueue,
        Channel<AudioFileQueueItem> audioFileQueue,
        QuotesContext db,
        HttpClient httpClient,
        ILogger<ShipProcessor> logger)
    {
        this.shipQueue = shipQueue;
        this.audioFileQueue = audioFileQueue;
        this.db = db;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        await foreach (var ship in shipQueue.Reader.ReadAllAsync(cancellationToken))
        {
            // Get quotes
            logger.LogInformation("Fetching wiki page for {Ship}", ship.EnglishName);
            using var document = await GetParseTree(ship.EnglishName, cancellationToken);

            List<Quote> quotes = new();

            foreach (var template in FindTemplates(document, new[] { "ShipquoteKai", "SeasonalQuote" }))
            {
                var missingParameters = new[] { "scenario", "translation", "origin" }.Where(x => !template.ContainsKey(x)).ToArray();
                if (missingParameters.Any())
                {
                    logger.LogWarning("One of {Ship}'s quotes is missing required parameters: {MissingParameters}",
                        ship.EnglishName, missingParameters);     

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

                // Add the audio file to the download queue
                if (quote.AudioFile is not null)
                {
                    await audioFileQueue.Writer.WriteAsync(new(quote), CancellationToken.None);
                }  
            }

            if (quotes.Count == 0)
            {
                throw new Exception($"No quotes found for {ship.EnglishName}. Check whether the scraping code is broken or the page actually has no quotes.");
            }

            // Save to db, replacing any existing quotes (as we can't identify changes to specific voice lines)
            logger.LogInformation("Saving {Count} quotes for {Ship}", quotes.Count, ship.EnglishName);
            db.Quotes.RemoveRange(await db.Quotes
                .Where(q => q.Source == Source.Kancolle && q.SpeakerEnglish == ship.EnglishName)
                .ToListAsync(cancellationToken));
            db.Quotes.AddRange(quotes);
            await db.SaveChangesAsync(cancellationToken);
        }

        // The ship queue is completed, so tell the audio worker it can complete too once its queue is empty
        audioFileQueue.Writer.Complete();
    }

    private async Task<IDocument> GetParseTree(string page, CancellationToken cancellationToken)
    {
        string url = new Url(WikiApiUrl).SetQueryParams(new
        {
            page,
            action = "parse",
            format = "json",
            prop = "parsetree",
            formatversion = 2
        });

        var response = await httpClient.GetFromJsonAsync<WikiApiResponse>(url, cancellationToken);
        var parseTree = response?.Parse?.ParseTree ?? throw new Exception($"Wiki API response for {page} missing 'parsetree'.");

        if (parseTree.StartsWith("<root>#REDIRECT"))
        {
            throw new Exception($"Wiki page {page} is a redirect, but redirects should have been filtered out by {nameof(ShipListProcessor)}.");
        }

        return await new XmlParser().ParseDocumentAsync(parseTree, cancellationToken);
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
        kaiName = null;

        if (template.ContainsKey("kai"))
            kaiName = "Kai";
        else if (template.ContainsKey("kai2"))
            kaiName = "Kai Ni";
        else if (template.ContainsKey("kai3"))
            kaiName = "Kai San";
        else if (template.TryGetString("form2", out var form2))
            kaiName = form2;
        else if (template.ContainsKey("kai2b"))
            kaiName = "Kai Ni B";
        else if (template.ContainsKey("kaic"))
            kaiName = "Kai Ni C";
        else if (template.ContainsKey("kai2c"))
            kaiName = "Kai Ni C";
        else if (template.ContainsKey("kai2d"))
            kaiName = "Kai Ni D";
        else if (template.ContainsKey("kai2e"))
            kaiName = "Kai Ni E";
        else if (template.ContainsKey("kai2toku"))
            kaiName = "Kai Ni Toku";
        else if (template.ContainsKey("kaigo"))
            kaiName = "Kai Ni Go";
        else if (template.ContainsKey("kai2j"))
            kaiName = "Kai Ni Juu";

        return kaiName is not null;
    }
}
