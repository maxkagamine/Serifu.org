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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    .WriteTo.File("../kancolle-warnings.log", restrictedToMinimumLevel: LogEventLevel.Warning));

builder.Services.AddSerifuLocalData();

builder.Services.AddSingleton<RateLimitingHttpHandler>();
builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<RateLimitingHttpHandler>();

builder.Services.AddScoped<ShipListService>();
builder.Services.AddScoped<ShipService>();
builder.Services.AddScoped<WikiApiService>();

builder.Services.AddEntryPoint<KancolleImporter>((importer, cancellationToken) =>
    importer.Import(cancellationToken));

await builder.Build().RunAsync();
