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
using Microsoft.AspNetCore.Mvc.Razor;
using Serifu.Web.Localization;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddMvcLocalization();

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Add("/Views/{0}.cshtml");
    options.ViewLocationFormats.Add("/Views/Components/{0}.cshtml");
    options.ViewLocationFormats.Add("/Views/Layouts/{0}.cshtml");
});

builder.Services.AddViteServices();

builder.Services.AddRequestLocalization(options =>
{
    options
        .AddSupportedCultures("en", "ja")
        .AddSupportedUICultures("en", "ja")
        .SetDefaultCulture("en");

    options.RequestCultureProviders = [
        new LocalizedRouteCultureProvider(), // Custom
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();

app.MapControllers()
    .WithLocalizedRoutes();

if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
    app.UseViteDevelopmentServer(useMiddleware: true);
}

app.Run();
