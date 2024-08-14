using AngleSharp;
using AngleSharp.Dom;
using Serifu.Importer.Kancolle.Models;

namespace Serifu.Importer.Kancolle;

/// <summary>
/// Handles requests to the wiki (rate-limited by handler set in Program).
/// </summary>
internal class WikiClient
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
