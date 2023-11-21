namespace Serifu.Importer.Kancolle.Models;

/// <summary>
/// Thrown if a requested wiki page is a redirect.
/// </summary>
internal class WikiRedirectException(string from, string to) : Exception($"{from} redirects to {to}.")
{ }