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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Importer;
using Serifu.Importer.Helpers;
using Serifu.Importer.Kancolle;

var builder = Host.CreateApplicationBuilder();

// Configure services
builder.Services.AddDbContext<QuotesContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Quotes")));
builder.Services.AddSingleton<RateLimitingHttpHandler>();
builder.Services.AddHttpClient(Options.DefaultName)
    .AddHttpMessageHandler<RateLimitingHttpHandler>();

builder.Services.AddChannel<ShipQueueItem>();
builder.Services.AddChannel<AudioFileQueueItem>();

builder.Services.AddScoped<ShipListProcessor>();
builder.Services.AddScoped<ShipProcessor>();
builder.Services.AddScoped<AudioFileProcessor>();

// Start the host. The hosted service infrastructure is only really good for workers that run forever (and I also wanted
// to be able to DI scoped services i.e. the DbContext instead of having to use the service locator), so I'm running my
// workers below instead; all Start() is really doing here is initializing the ConsoleLifetime's Ctrl+C handler.
var host = builder.Build();
host.Start();

// Create the database
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuotesContext>();
    await db.Database.MigrateAsync();
}

// Start each worker on a separate thread. It's a bit overkill here, but the idea is one worker will fetch and add ships
// to a pub-sub queue which another worker will process; meanwhile a third worker will be downloading the audio files
// added to another queue. If anything fails (very likely due to the nature of web scraping), other workers can finish
// what they're doing before the program quits, and re-running it should pick up where it left off.
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

logger.LogInformation("Current directory is {CurrentDirectory}", Environment.CurrentDirectory);

await Task.WhenAll(builder.Services
    .Where(s => s.ServiceType.IsAssignableTo(typeof(IProcessor)))
    .Select(s => Task.Run(async () =>
    {
        using var scope = host.Services.CreateScope();
        var worker = (IProcessor)scope.ServiceProvider.GetRequiredService(s.ServiceType);

        try
        {
            logger.LogInformation("{Worker} starting.", s.ServiceType.Name);
            await worker.Run(lifetime.ApplicationStopping);
            logger.LogInformation("{Worker} completed.", s.ServiceType.Name);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("{Worker} cancelled.", s.ServiceType.Name);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{Worker} failed, stopping application.", s.ServiceType.Name);
            lifetime.StopApplication();
            throw;
        }
    })));