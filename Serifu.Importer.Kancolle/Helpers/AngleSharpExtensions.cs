using AngleSharp.Dom;

namespace Serifu.Importer.Kancolle.Helpers;

internal static class AngleSharpExtensions
{
    /// <summary>
    /// Gets the content of an element's text nodes, i.e. the inner text excluding children, and returns the trimmed
    /// string.
    /// </summary>
    public static string GetTextNodes(this IElement element)
        => string.Join("", element.ChildNodes
            .Where(node => node.NodeType == NodeType.Text)
            .Select(node => node.TextContent)).Trim();
}
