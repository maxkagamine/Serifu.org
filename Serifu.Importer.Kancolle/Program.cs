using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Importer.Kancolle;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Services;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSerilog(config => config
    .MinimumLevel.Debug()
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("kancolle-warnings.log", restrictedToMinimumLevel: LogEventLevel.Warning));

builder.Services.AddDbContext<QuotesContext>(options => options.UseSqlite("Data Source=quotes.db"));

builder.Services.AddSingleton<RateLimitingHttpHandler>();
builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<RateLimitingHttpHandler>();

builder.Services.AddScoped<AudioFileService>();
builder.Services.AddScoped<QuotesService>();
builder.Services.AddScoped<ShipListService>();
builder.Services.AddScoped<ShipService>();
builder.Services.AddScoped<WikiApiService>();

builder.Services.AddEntryPoint<KancolleImporter>((importer, cancellationToken) =>
    importer.Import(cancellationToken));

await builder.Build().RunAsync();
