using System.Net.Http.Json;
using AngleSharp;
using AngleSharp.Dom;
using Serifu.Importer.Kancolle.Models;
using Url = Flurl.Url;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles interaction with the MediaWiki API.
/// </summary>
internal partial class WikiApiService
{
    const string WikiApiUrl = "https://en.kancollewiki.net/w/api.php";
    private static readonly Uri WikiBaseUri = new("https://en.kancollewiki.net/");

    private readonly HttpClient httpClient;

    public WikiApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Fetches and parses the given wiki page using the API.
    /// </summary>
    /// <param name="page">The wiki page to fetch.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The parsed HTML document.</returns>
    /// <exception cref="WikiRedirectException">The requested wiki page is a redirect.</exception>
    public async Task<IDocument> GetPage(string page, CancellationToken cancellationToken = default)
    {
        string url = new Url(WikiApiUrl).SetQueryParams(new
        {
            page,
            action = "parse",
            format = "json",
            prop = "text",
            formatversion = 2
        });

        var response = await httpClient.GetFromJsonAsync<WikiApiResponse>(url, cancellationToken);
        var html = response?.Parse?.Text ?? throw new Exception($"Wiki API returned an invalid response for {page}.");
        var document = await new BrowsingContext().OpenAsync(res => res
            .Address(new Uri(WikiBaseUri, page.Replace(' ', '_'))).Content(html), cancellationToken);

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
