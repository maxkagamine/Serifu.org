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
