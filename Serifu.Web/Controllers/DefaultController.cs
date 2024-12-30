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
using Serifu.Web.Localization;
using Serifu.Web.Models;
using System.Globalization;

namespace Serifu.Web.Controllers;

public class DefaultController : Controller
{
    private readonly IElasticsearchService elasticsearch;
    private readonly IOptions<SerifuOptions> options;

    public DefaultController(IElasticsearchService elasticsearch, IOptions<SerifuOptions> options)
    {
        this.elasticsearch = elasticsearch;
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
            return RedirectToAction(nameof(Results), new { query });
        }

        return View();
    }

    [HttpGet("/translate/{query}")]
    [HttpGet("/翻訳/{query}")]
    public async Task<ActionResult> Results(string query, CancellationToken cancellationToken)
    {
        SearchResults results = await elasticsearch.Search(query, cancellationToken);
        bool englishFirst = results.SearchLanguage == SearchLanguage.English;
        string audioFileBaseUrl = options.Value.AudioFileBaseUrl;

        ResultsViewModel model = new(results, englishFirst, audioFileBaseUrl);

        return View(model);
    }

    [HttpGet("/about")]
    [HttpGet("/について")]
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
