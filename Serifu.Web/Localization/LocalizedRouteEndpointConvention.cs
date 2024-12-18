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

using Microsoft.AspNetCore.Routing.Patterns;
using System.Text.RegularExpressions;

namespace Serifu.Web.Localization;

/// <summary>
/// Adds the route language to each endpoint's route values. By leveraging the built-in route values, when linking to an
/// action (using IUrlHelper or the built-in tag helpers) it will automatically choose the route corresponding to the
/// current language (using "ambient values" from the current route) unless a "lang" is explicitly specified, the same
/// way that "controller" defaults to the current controller.
/// </summary>
public static partial class LocalizedRouteEndpointConvention
{
    public const string LanguageRouteValueKey = "lang";

    [GeneratedRegex(@"[一-龠ぁ-ゔァ-ヴー々〆〤ヶ]")]
    private static partial Regex JapaneseCharacters { get; }

    public static IEndpointConventionBuilder WithLocalizedRoutes(this IEndpointConventionBuilder builder)
    {
        builder.Add(ApplyLocalizedRouteConvention);
        return builder;
    }

    private static void ApplyLocalizedRouteConvention(EndpointBuilder endpoint)
    {
        if (endpoint is not RouteEndpointBuilder route)
        {
            return;
        }

        RoutePattern routePattern = route.RoutePattern;

        // We don't set a language for the index so that the culture provider falls back to cookie or Accept-Language,
        // which the index action can use to redirect to the appropriate homepage for the user.
        if (!routePattern.PathSegments.Any())
        {
            return;
        }

        // Since the only languages we support are English and Japanese, we can cheat a bit here and detect the route
        // language simply by whether or not it contains Japanese. (For a solution that supports arbitrary languages,
        // look into application model conventions to define a custom [LocalizedRoute] attribute.)
        string lang = routePattern.RawText is not null && JapaneseCharacters.IsMatch(routePattern.RawText) ?
            "ja" : "en";

        // Add the language to the route values, alongside "controller" and "action". RoutePattern's ctor is internal,
        // and RoutePatternFactory doesn't provide a way to clone/combine an existing pattern with addl RequiredValues,
        // so we have to mutate the existing pattern's collections.
        ((IDictionary<string, object>)routePattern.Defaults)[LanguageRouteValueKey] = lang;
        ((IDictionary<string, object>)routePattern.RequiredValues)[LanguageRouteValueKey] = lang;
    }
}
