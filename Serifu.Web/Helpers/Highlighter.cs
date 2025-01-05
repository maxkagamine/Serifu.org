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

using Microsoft.AspNetCore.Html;

namespace Serifu.Web.Helpers;

public static class Highlighter
{
    private const string OpenTag = "<mark>";
    private const string CloseTag = "</mark>";

    /// <summary>
    /// HTML-encodes <paramref name="text"/> while wrapping the highlight ranges in tags.
    /// </summary>
    /// <param name="text">The text to highlight.</param>
    /// <param name="highlights">The highlight ranges. Must be sorted and non-overlapping.</param>
    /// <returns>An <see cref="IHtmlContent"/> that can be used directly in a view.</returns>
    public static IHtmlContent ApplyHighlights(string text, IReadOnlyList<Range> highlights)
    {
        if (highlights.Count == 0)
        {
            return new HtmlString(text);
        }

        HtmlContentBuilder highlightedText = new(highlights.Count * 4 + 1); // May be one or two less if a highlight is at the start/end, but highlights won't be directly touching
        Index prevEnd = 0;

        foreach (var highlight in highlights)
        {
            highlightedText.Append(text[prevEnd..highlight.Start]);
            highlightedText.AppendHtml(OpenTag);
            highlightedText.Append(text[highlight]);
            highlightedText.AppendHtml(CloseTag);

            prevEnd = highlight.End;
        }

        highlightedText.Append(text[highlights[^1].End..]);

        return highlightedText;
    }
}
