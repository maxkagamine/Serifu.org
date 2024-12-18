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

using Microsoft.AspNetCore.Localization;

namespace Serifu.Web.Localization;

/// <summary>
/// Sets the culture to the language defined in the route values by <see cref="LocalizedRouteEndpointConvention"/>.
/// </summary>
public class LocalizedRouteCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ProviderCultureResult? result = null;

        if (httpContext.GetRouteValue(LocalizedRouteEndpointConvention.LanguageRouteValueKey) is string lang)
        {
            result = new ProviderCultureResult(lang);
        }

        return Task.FromResult(result);
    }
}
