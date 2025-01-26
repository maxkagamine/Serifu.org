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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Serifu.Data.Elasticsearch;
using Serifu.Web;
using Serifu.Web.Helpers;
using Serifu.Web.Localization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Unicode;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Services.AddSerilog((IServiceProvider provider, LoggerConfiguration config) =>
{
    var appsettings = provider.GetRequiredService<IConfiguration>();
    var levelSwitch = new LoggingLevelSwitch();

    config
        .MinimumLevel.ControlledBy(levelSwitch)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection", LogEventLevel.Error)
        .Enrich.WithProperty("Application", "Serifu.Web")
        .Enrich.FromLogContext()
        .WriteTo.Seq(
            serverUrl: appsettings["SeqUrl"] ?? throw new Exception("SeqUrl not set in configuration"),
            apiKey: appsettings["SeqApiKey"] ?? throw new Exception("SeqApiKey not set in configuration"),
            controlLevelSwitch: levelSwitch)
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

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
    // https://github.com/dotnet/aspnetcore/issues/39984
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

        if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
        {
            if (contentType.StartsWith("image/"))
            {
                // Getting the background image to look right (correct color, no banding) was a struggle, and it will
                // almost definitely get messed up if a mobile browser's data saver tries to re-"optimize" it.
                ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable, no-transform";
            }
            else
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            }
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

builder.Services.AddSingleton<IUrlSigner>(provider =>
{
    var options = provider.GetRequiredService<IOptions<SerifuOptions>>().Value.AudioFiles;
    return string.IsNullOrEmpty(options.SigningKey) ?
        new NoopUrlSigner() :
        new CloudFrontUrlSigner(options.SigningKey, options.KeyPairId);
});

builder.Services.AddSingleton<AudioFileUrlProvider>();

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Trigger a warning when responses are slow
    var threshold = app.Configuration.GetValue<TimeSpan>("ResponseTimeWarningThreshold").TotalMilliseconds;
    options.GetLevel = (context, elapsed, ex) =>
        ex is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error :
        elapsed >= threshold ? LogEventLevel.Warning :
        LogEventLevel.Information;
});

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
