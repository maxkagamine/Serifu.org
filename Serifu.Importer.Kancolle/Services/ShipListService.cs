﻿using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Models;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the "Ship list" page.
/// </summary>
internal class ShipListService
{
    const string ShipListPage = "Ship list";

    private readonly WikiApiService wikiApiService;
    private readonly ILogger<ShipListService> logger;

    public ShipListService(
        WikiApiService wikiApiService,
        ILogger<ShipListService> logger)
    {
        this.wikiApiService = wikiApiService;
        this.logger = logger;
    }

    /// <summary>
    /// Parses the wiki's Ship List page and returns the list of ships, excluding remodels (which redirect to the same
    /// page as the base ship).
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of <see cref="Ship"/>.</returns>
    public async Task<IEnumerable<Ship>> GetShips(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting ship list.");
        using var document = await wikiApiService.GetHtml(ShipListPage, cancellationToken);

        var ships = document.QuerySelectorAll<IHtmlAnchorElement>("span[id^=\"shiplistkai-\"] a:not(.mw-redirect)")
            .Select(link =>
            {
                var englishName = link.TextContent.Trim();
                var japaneseName = link.ParentElement?.GetTextNodes() ?? englishName;

                return new Ship(englishName, japaneseName);
            })
            .ToList();

        if (ships.Count == 0)
        {
            throw new Exception("Found zero ships. This probably means the CSS selector is broken.");
        }

        if (!ships.Any(s => s.JapaneseName != s.EnglishName)) // Japanese name is actually "name shown in game" which might be English or Cyrillic
        {
            throw new Exception("No ship has a Japanese name. This probably means the table structure has changed.");
        }

        logger.LogInformation("Found {Count} ships.", ships.Count);
        return ships;
    }
}
