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

using Elastic.Clients.Elasticsearch;
using Kagamine.Extensions.Hosting;
using Serifu.Data.Elasticsearch;
using Serifu.Data.Sqlite;
using Serilog;
using Serilog.Events;

var builder = ConsoleApplication.CreateBuilder();

builder.Services.AddSerilog(config => config
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}"));

builder.Services.AddSerifuElasticsearch("http://localhost:9200");
builder.Services.AddSerifuSqlite("Data Source=../../../../Serifu.db"); // TODO: Replace with path in container

builder.Run(async (
    ElasticsearchClient elasticsearch,
    SerifuDbContext sqlite,
    ILogger logger,
    CancellationToken cancellationToken) =>
{
    logger.Information("Waiting for elasticsearch to start...");

    while (true)
    {
        try
        {
            await elasticsearch.PingAsync(cancellationToken);
            break;
        }
        catch
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    logger.Information("Ready.");
});
