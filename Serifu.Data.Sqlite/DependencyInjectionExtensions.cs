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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Serifu.Data.Sqlite;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuSqlite(this IServiceCollection services)
    {
        services.AddDbContext<SerifuContext>();
        services.AddScoped<ISqliteService, SqliteService>();

        return services;
    }

    // Not strictly "data" but it's convenient here since all importers will reference this project
    public static IServiceCollection AddSerifuSerilog(
        this IServiceCollection services,
        Action<LoggerConfiguration>? configureLogger = null,
        Action<LoggerConfiguration>? configureConsoleLogger = null)
        => services.AddSerilog((IServiceProvider provider, LoggerConfiguration config) =>
        {
            string appName = provider.GetRequiredService<IHostEnvironment>().ApplicationName;

            string seqUrl = provider.GetRequiredService<IConfiguration>()["SeqUrl"] ??
                throw new Exception("SeqUrl not set in configuration (user secrets)");

            config
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Application", appName)
                .Enrich.WithProperty("InvocationId", Guid.NewGuid())
                .WriteTo.Logger(consoleLogger =>
                {
                    consoleLogger
                        .Filter.ByExcluding(logEvent => logEvent.Level < LogEventLevel.Warning && (
                            Matching.FromSource("System")(logEvent) ||
                            Matching.FromSource("Microsoft")(logEvent)))
                        .WriteTo.Console();

                    configureConsoleLogger?.Invoke(config);
                })
                .WriteTo.Seq(seqUrl);

            configureLogger?.Invoke(config);
        });
}
