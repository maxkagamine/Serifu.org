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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Data.Elasticsearch;
using Serifu.Web.Helpers;
using Serifu.Web.Localization;
using Serifu.Web.Models;
using System.Diagnostics;
using System.Globalization;

namespace Serifu.Web.Controllers;

public class DefaultController : Controller
{
    private readonly IElasticsearchService elasticsearch;
    private readonly AudioFileUrlProvider audioFileUrlProvider;
    private readonly IOptions<SerifuOptions> options;

    public DefaultController(
        IElasticsearchService elasticsearch,
        AudioFileUrlProvider audioFileUrlProvider,
        IOptions<SerifuOptions> options)
    {
        this.elasticsearch = elasticsearch;
        this.audioFileUrlProvider = audioFileUrlProvider;
        this.options = options;
    }

    [HttpGet("/")]
    public ActionResult Index()
    {
        // This is the only route that won't have a "lang" route value set. This causes the culture provider to fall
        // back to using a cookie if set and Accept-Language otherwise; we can then use the determined culture to
        // redirect to the appropriate homepage.
        return RedirectToAction(nameof(Home), new { lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName });
    }

    [HttpGet("/translate")]
    [HttpGet("/翻訳")]
    public ActionResult Home(string? query)
    {
        if (query is not null)
        {
            // Fallback in case user has JS disabled
            Response.Headers.Append("X-Robots-Tag", "noindex");
            return RedirectToAction(nameof(Results), new { query = query.Trim() });
        }

        return View();
    }

    [HttpGet("/translate/{query}")]
    [HttpGet("/翻訳/{query}")]
    public async Task<ActionResult> Results(string? query, CancellationToken cancellationToken)
    {
        try
        {
            query ??= "";
            SearchResults results = await elasticsearch.Search(query, cancellationToken);
            bool englishFirst = results.SearchLanguage == SearchLanguage.English;

            ResultsViewModel model;

            if (results.Count > 0)
            {
                model = new()
                {
                    Quotes = results.AsParallel().AsOrdered()
                        .Select(r => new QuoteViewModel(r, englishFirst, audioFileUrlProvider))
                        .ToArray()
                };

                // Ensure the page expires from cache well before its links become invalid
                if (audioFileUrlProvider.Ttl != TimeSpan.Zero)
                {
                    Response.GetTypedHeaders().Expires = DateTime.UtcNow + (audioFileUrlProvider.Ttl / 2);
                }
            }
            else
            {
                model = new()
                {
                    ErrorMessage = Strings.FormatNoResults(query.Trim())
                };

                // Since each search results page is technically its own resource (using route instead of query param),
                // we can get away with returning 404 to prevent "no results" from appearing in Google.
                Response.StatusCode = StatusCodes.Status404NotFound;
            }

            ViewBag.Title = englishFirst ?
                Strings.FormatSearchPageTitle_English(query) :
                Strings.FormatSearchPageTitle_Japanese(query);

            ViewBag.MetaDescription = englishFirst ?
                Strings.FormatMetaDescription_Search_English(query) :
                Strings.FormatMetaDescription_Search_Japanese(query);

            return View(model);
        }
        catch (ElasticsearchValidationException ex)
        {
            ResultsViewModel model = new()
            {
                ErrorMessage = ex.Error switch
                {
                    ElasticsearchValidationError.TooShort => Strings.ValidationErrorTooShort,
                    ElasticsearchValidationError.TooLong => Strings.ValidationErrorTooLong,
                    _ => throw new UnreachableException($"Unknown validation error: {ex.Error}")
                }
            };

            Response.StatusCode = StatusCodes.Status400BadRequest;
            ViewBag.Title = Strings.ErrorPageTitle;
            return View(model);
        }
    }

    [HttpGet("/about")]
    [HttpGet("/とは")]
    public ActionResult About()
    {
        AboutPageViewModel model = new()
        {
            GameListRows = Enum.GetValues<Source>()
                .Select(s => new GameListRow()
                {
                    Source = s,
                    Game = Strings.GetResourceString($"SourceTitle_{s}") ?? throw new Exception($"No source title for {s}."),
                    Copyright = Strings.GetResourceString($"SourceCopyright_{s}") ?? throw new Exception($"No source copyright for {s}."),
                    Links = options.Value.SourceLinks[s]
                        .Where(link =>
                            link.Language is null ||
                            link.Language == CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
                        .ToList()
                })
                .OrderBy(r => r.Game, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
        };

        return View(model);
    }
}
