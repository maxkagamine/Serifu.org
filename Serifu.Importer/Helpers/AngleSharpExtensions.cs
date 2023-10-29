using AngleSharp.Dom;

namespace Serifu.Importer.Helpers;

internal static class AngleSharpExtensions
{
    public static IEnumerable<IElement> GetChildren(this IElement element, string tagName)
        => element.Children.Where(el => el.TagName == tagName);

    public static IElement GetChild(this IElement element, string tagName)
        => element.GetChildren(tagName).Single();

    /// <summary>
    /// Gets the content of an element's text nodes, i.e. the inner text excluding children, and returns the trimmed
    /// string.
    /// </summary>
    public static string GetTextNodes(this IElement element)
        => string.Join("", element.ChildNodes
            .Where(node => node.NodeType == NodeType.Text)
            .Select(node => node.TextContent)).Trim();
}
