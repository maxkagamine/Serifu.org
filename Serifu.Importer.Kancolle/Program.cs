using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Importer.Kancolle;
using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Services;

var builder = Host.CreateApplicationBuilder();

builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

builder.Services.AddDbContext<QuotesContext>(options => options.UseSqlite("Data Source=quotes.db"));

builder.Services.AddSingleton<RateLimitingHttpHandler>();
builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<RateLimitingHttpHandler>();

builder.Services.AddScoped<AudioFileService>();
builder.Services.AddScoped<QuotesService>();
builder.Services.AddScoped<ShipListService>();
builder.Services.AddScoped<ShipService>();
builder.Services.AddScoped<WikiApiService>();

builder.Services.AddHostedService<KancolleImporter>();

await builder.Build().RunAsync();
