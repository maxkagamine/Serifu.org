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
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Serifu.Data.Sqlite;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuSqlite(this IServiceCollection services,
        string connectionString = "Data Source=../Serifu.db")
    {
        services.AddDbContextFactory<SerifuDbContext>(options => options
            .UseSqlite(connectionString));

        services.AddHttpClient();

        services.AddScoped<ISqliteService, SqliteService>();

        return services;
    }

    // Not strictly "data" but it's convenient here since all importers will reference this project
    public static IServiceCollection AddSerifuSerilog(
        this IServiceCollection services,
        Action<IServiceProvider, LoggerConfiguration>? configureLogger = null)
        => services.AddSerilog((IServiceProvider provider, LoggerConfiguration config) =>
        {
            string appName = provider.GetRequiredService<IHostEnvironment>().ApplicationName;
            var appsettings = provider.GetRequiredService<IConfiguration>();

            config
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Application", appName)
                .Enrich.WithProperty("InvocationId", Guid.NewGuid())
                .Enrich.FromLogContext()
                .WriteTo.Logger(x => x
                    .MinimumLevel.Information()
                    .Filter.ByExcluding(logEvent => logEvent.Level < LogEventLevel.Warning && (
                        Matching.FromSource("System")(logEvent) ||
                        Matching.FromSource("Microsoft")(logEvent)))
                    .WriteTo.Console());

            if (!EF.IsDesignTime)
            {
                config.WriteTo.Seq(
                    serverUrl: appsettings["SeqUrl"] ?? throw new Exception("SeqUrl not set in configuration"),
                    apiKey: appsettings["SeqApiKey"] ?? throw new Exception("SeqApiKey not set in configuration"));
            }

            configureLogger?.Invoke(provider, config);
        });
}
