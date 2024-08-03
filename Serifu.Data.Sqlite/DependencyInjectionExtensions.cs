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
            string? seqUrl = provider.GetRequiredService<IConfiguration>()["SeqUrl"];

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
                config.WriteTo.Seq(seqUrl ?? throw new Exception("SeqUrl not set in configuration (user secrets)"));
            }

            configureLogger?.Invoke(provider, config);
        });
}
