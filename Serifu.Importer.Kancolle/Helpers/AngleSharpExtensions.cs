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

namespace Serifu.Importer.Kancolle.Helpers;

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
