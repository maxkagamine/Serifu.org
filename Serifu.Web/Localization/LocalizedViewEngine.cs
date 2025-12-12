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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Serifu.Web.Localization;

/// <summary>
/// This is added to the <see cref="CompositeViewEngine"/> to enable localized views for text-heavy pages.
/// </summary>
internal sealed class LocalizedViewEngine : IViewEngine, IConfigureOptions<MvcViewOptions>
{
    private readonly IRazorViewEngine razorViewEngine;

    public LocalizedViewEngine(IRazorViewEngine razorViewEngine)
    {
        this.razorViewEngine = razorViewEngine;
    }

    public void Configure(MvcViewOptions options)
    {
        options.ViewEngines.Add(this);
    }

    public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
    {
        return razorViewEngine.FindView(context, $"{viewName}.{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}", isMainPage);
    }

    public ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage)
    {
        return ViewEngineResult.NotFound(viewPath, []);
    }
}

internal static class LocalizedViewEngineExtensions
{
    public static IMvcBuilder AddLocalizedViews(this IMvcBuilder builder)
    {
        builder.Services.AddTransient<IConfigureOptions<MvcViewOptions>, LocalizedViewEngine>();
        return builder;
    }
}
