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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Serifu.Importer.Helpers;

namespace Serifu.Importer.Kancolle;
internal class WikiTemplate
{
    private readonly Dictionary<string, IElement> parameters;

    public WikiTemplate(IElement element)
    {
        if (element.TagName != "template")
        {
            throw new ArgumentException($"{nameof(element)} is not a template tag.");
        }

        Name = element.GetChild("title").GetTextNodes();
        parameters = element.GetChildren("part").ToDictionary(
            part =>
            {
                var name = part.GetChild("name");
                return name.GetAttribute("index") ?? name.GetTextNodes();
            },
            part => part.GetChild("value"));
    }

    /// <summary>
    /// The template name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Determines whether the template contains the specified parameter.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    public bool ContainsKey(string key) => parameters.ContainsKey(key);

    /// <summary>
    /// Gets the value node for the specified parameter.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <exception cref="KeyNotFoundException"/>
    public IElement GetXml(string key) => parameters[key];

    /// <summary>
    /// Gets the trimmed string value of the specified parameter, excluding any child elements such as &lt;ext&gt;.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <exception cref="KeyNotFoundException"/>
    public string this[string key] => GetString(key);

    /// <summary>
    /// Gets the trimmed string value of the specified parameter, excluding any child elements such as &lt;ext&gt;.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <exception cref="KeyNotFoundException"/>
    public string GetString(string key) => parameters[key].GetTextNodes();

    /// <summary>
    /// Tries to get the trimmed string value of the specified parameter, excluding any child elements such as
    /// &lt;ext&gt;.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <param name="defaultValue">The default value to return if the parameter does not exist.</param>
    public string? GetStringOrDefault(string key, string? defaultValue = null)
        => parameters.GetValueOrDefault(key)?.GetTextNodes() ?? defaultValue;

    /// <summary>
    /// Tries to get the trimmed string value of the specified parameter, excluding any child elements such as
    /// &lt;ext&gt;.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <param name="value">The parameter value, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the template contains the specified parameter; otherwise, <see langword="false"/>.</returns>
    public bool TryGetString(string key, [NotNullWhen(true)] out string? value)
    {
        if (parameters.TryGetValue(key, out var element))
        {
            value = element.GetTextNodes();
            return true;
        }

        value = default;
        return false;
    }
}
