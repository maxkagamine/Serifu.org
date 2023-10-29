namespace Serifu.Importer.Kancolle.Models;

/// <summary>
/// Thrown if a requested wiki page is a redirect. We could follow redirects, but in this case it means we mistakenly
/// followed a link to a Kai, which redirects to the base ship's page and so would result in duplicates.
/// </summary>
internal class WikiRedirectException : Exception
{
    public WikiRedirectException(string from, string to)
        : base($"{from} redirects to {to}.")
    { }
}