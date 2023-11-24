using Serifu.Data.Local;

namespace Microsoft.Extensions.DependencyInjection;
public static class LocalDataDependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuLocalData(this IServiceCollection services)
    {
        services.AddDbContext<QuotesContext>();
        services.AddScoped<ILocalDataService, LocalDataService>();

        return services;
    }
}
