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

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xml.Dom;
using AngleSharp.Xml.Parser;
using Serifu.Importer.Kancolle.Models;
using Serilog;
using Url = Flurl.Url;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Handles interaction with the MediaWiki API.
/// </summary>
internal partial class WikiApiService
{
    const string WikiApiUrl = "https://en.kancollewiki.net/w/api.php";

    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    [GeneratedRegex(@"^<root>#REDIRECT \[\[([^\]]+)")]
    private static partial Regex RedirectParseTreeRegex();

    public WikiApiService(
        HttpClient httpClient,
        ILogger logger)
    {
        this.httpClient = httpClient;
        this.logger = logger.ForContext<WikiApiService>();
    }

    public async Task<IHtmlDocument> GetHtml(string page, CancellationToken cancellationToken = default)
    {
        var response = await SendParseRequest(page, "text", cancellationToken);
        var html = response.Parse?.Text ?? throw new Exception($"Wiki API response for {page} is missing 'text' prop.");
        var document = await new HtmlParser().ParseDocumentAsync(html, cancellationToken);

        var redirectLink = document.QuerySelector(".mw-parser-output > .redirectMsg a");
        if (redirectLink is not null)
        {
            throw new WikiRedirectException(page, redirectLink.GetAttribute("title") ?? "");
        }

        return document;
    }

    public async Task<IXmlDocument> GetXml(string page, CancellationToken cancellationToken = default)
    {
        var response = await SendParseRequest(page, "parsetree", cancellationToken);
        var xml = response.Parse?.ParseTree ?? throw new Exception($"Wiki API response for {page} is missing 'parsetree' prop.");

        var redirectMatch = RedirectParseTreeRegex().Match(xml);
        if (redirectMatch.Success)
        {
            throw new WikiRedirectException(page, redirectMatch.Groups[1].Value);
        }
        
        return await new XmlParser().ParseDocumentAsync(xml, cancellationToken);
    }

    private async Task<WikiApiResponse> SendParseRequest(string page, string prop, CancellationToken cancellationToken = default)
    {
        string url = new Url(WikiApiUrl).SetQueryParams(new
        {
            page,
            action = "parse",
            format = "json",
            prop,
            formatversion = 2
        });

        return await httpClient.GetFromJsonAsync<WikiApiResponse>(url, cancellationToken) ??
            throw new Exception($"Wiki API response for {page} and prop {prop} is null.");
    }
}
