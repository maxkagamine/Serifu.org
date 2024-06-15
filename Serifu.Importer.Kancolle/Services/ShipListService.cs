﻿using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Serifu.Importer.Kancolle.Models;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles scraping the "Ship list" page.
/// </summary>
internal class ShipListService
{
    const string ShipListPage = "Ship list";

    private readonly WikiApiService wikiApiService;
    private readonly ILogger logger;

    public ShipListService(
        WikiApiService wikiApiService,
        ILogger logger)
    {
        this.wikiApiService = wikiApiService;
        this.logger = logger.ForContext<ShipListService>();
    }

    /// <summary>
    /// Parses the wiki's Ship List page and returns the list of ships, excluding remodels (which redirect to the same
    /// page as the base ship).
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of <see cref="Ship"/>.</returns>
    public async Task<IEnumerable<Ship>> GetShips(CancellationToken cancellationToken = default)
    {
        logger.Information("Getting ship list.");
        using var document = await wikiApiService.GetPage(ShipListPage, cancellationToken);

        var ships = document.QuerySelectorAll<IHtmlAnchorElement>("span[id^=\"shiplistkai-\"] a:not(.mw-redirect)")
            .DistinctBy(link => link.Href) // Verniy links directly to Hibiki in the table, so isn't a redirect
            .Select(link =>
            {
                var englishName = link.TextContent.Trim();
                var japaneseName = GetTextNodes(link.ParentElement);

                var shipNumberStr = GetTextNodes(link.Closest("tr")?.FirstElementChild);
                if (shipNumberStr is null || !int.TryParse(shipNumberStr, out int shipNumber))
                {
                    throw new Exception($"Failed to extract ship number for {englishName}. The table may have changed.");
                }

                return new Ship(shipNumber, englishName, string.IsNullOrEmpty(japaneseName) ? englishName : japaneseName);
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

        logger.Information("Found {Count} ships.", ships.Count);
        return ships;
    }

    /// <summary>
    /// Gets the content of an element's text nodes (the inner text excluding children) and returns the trimmed string,
    /// or null if <paramref name="element"/> is null.
    /// </summary>
    public static string? GetTextNodes(IElement? element)
        => element is null ? null : string.Join("", element.ChildNodes
            .Where(node => node.NodeType == NodeType.Text)
            .Select(node => node.TextContent)).Trim();
}
