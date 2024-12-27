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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.WebEncoders;
using Serifu.Web;
using Serifu.Web.Localization;
using System.Text.Unicode;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<SerifuOptions>().BindConfiguration("");

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Add("/Views/{0}.cshtml");
    options.ViewLocationFormats.Add("/Views/Components/{0}.cshtml");
    options.ViewLocationFormats.Add("/Views/Layouts/{0}.cshtml");
});

builder.Services.Configure<WebEncoderOptions>(options =>
{
    // Don't need to html-encode every single Japanese character
    options.TextEncoderSettings = new(UnicodeRanges.All);
});

builder.Services.AddControllersWithViews()
    .AddMvcLocalization();

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
    app.Environment.WebRootPath = Path.Combine(app.Environment.ContentRootPath, "Assets", "public");
    app.Environment.WebRootFileProvider = new PhysicalFileProvider(app.Environment.WebRootPath);

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
