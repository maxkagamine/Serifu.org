using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Serifu.Importer.Kancolle.Models;

/// <summary>
/// Represents the table rows created by the ShipquoteKai and SeasonalQuote templates, and the relevant elements
/// contained therein.
/// </summary>
/// <param name="Scenario">The element containing the "scenario" name.</param>
/// <param name="PlayButton">The play button, if the quote has audio.</param>
/// <param name="English">The cell containing the English translation.</param>
/// <param name="Japanese">The cell containing the original Japanese.</param>
/// <param name="Notes">The cell containing notes, if a SeasonalQuote template.</param>
internal record struct QuoteTableRow(
    IElement Scenario,
    IHtmlAnchorElement? PlayButton,
    IElement English,
    IElement Japanese,
    IElement? Notes);
