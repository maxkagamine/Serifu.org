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
