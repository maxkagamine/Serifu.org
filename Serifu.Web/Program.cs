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
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.WebEncoders;
using Serifu.Data.Elasticsearch;
using Serifu.Web;
using Serifu.Web.Helpers;
using Serifu.Web.Localization;
using Serilog;
using System.Text.Unicode;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config
    .ReadFrom.Configuration(builder.Configuration));

builder.Services.AddOptions<SerifuOptions>().BindConfiguration("").ValidateDataAnnotations();

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

builder.Services.Configure<StaticFileOptions>(options =>
{
    options.ContentTypeProvider = new FileExtensionContentTypeProvider()
    {
        Mappings =
        {
            [".avif"] = "image/avif"
        }
    };

    options.OnPrepareResponse = ctx =>
    {
        string contentType = ctx.Context.Response.Headers.ContentType.ToString();

        if (contentType.StartsWith("text/"))
        {
            ctx.Context.Response.Headers.ContentType = $"{contentType}; charset=utf-8";
        }
    };
});

builder.Services.AddControllersWithViews()
    .AddMvcLocalization()
    .AddLocalizedViews();

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

builder.Services.AddSerifuElasticsearch();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(730);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddHeaders(headers =>
{
    headers.ContentSecurityPolicy = "frame-ancestors 'none'";
    headers.XPoweredBy = "Rin-chan";
});

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler("/Error/500");
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHsts();
app.UseHeaders();

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
