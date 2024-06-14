using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Data.Sqlite;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuSqlite(this IServiceCollection services)
    {
        services.AddDbContext<SerifuContext>();
        services.AddScoped<ISqliteService, SqliteService>();

        return services;
    }
}
