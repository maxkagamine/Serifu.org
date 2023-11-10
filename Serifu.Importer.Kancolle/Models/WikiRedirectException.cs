namespace Serifu.Importer.Kancolle.Models;

/// <summary>
/// Thrown if a requested wiki page is a redirect.
/// </summary>
internal class WikiRedirectException : Exception
{
    public WikiRedirectException(string from, string to)
        : base($"{from} redirects to {to}.")
    { }
}