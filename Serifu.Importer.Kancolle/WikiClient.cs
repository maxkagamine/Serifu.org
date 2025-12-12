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

using AngleSharp;
using AngleSharp.Dom;
using Serifu.Importer.Kancolle.Models;

namespace Serifu.Importer.Kancolle;

/// <summary>
/// Handles requests to the wiki (rate-limited by handler set in Program).
/// </summary>
internal sealed class WikiClient
{
    private static readonly Uri WikiBaseUri = new("https://en.kancollewiki.net/");

    private readonly HttpClient httpClient;

    public WikiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Fetches and parses the given wiki page.
    /// </summary>
    /// <param name="page">The wiki page to fetch.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The parsed HTML document.</returns>
    /// <exception cref="WikiRedirectException">The requested wiki page is a redirect.</exception>
    public async Task<IDocument> GetPage(string page, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(WikiBaseUri, page.Replace(' ', '_'));

        string html = await httpClient.GetStringAsync(uri, cancellationToken);
        var document = await new BrowsingContext().OpenAsync(res => res
            .Address(uri).Content(html), cancellationToken);

        // We could follow redirects, but in this case it means we mistakenly followed a link to a remodel which
        // redirects to the base ship's page and thus would result in duplicates.
        var redirectLink = document.QuerySelector(".mw-parser-output > .redirectMsg a");
        if (redirectLink is not null)
        {
            throw new WikiRedirectException(page, redirectLink.GetAttribute("title") ?? "");
        }

        return document;
    }
}
