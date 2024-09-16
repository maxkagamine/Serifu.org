using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Analysis;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuAnalysis(this IServiceCollection services)
    {
        services.AddSingleton<VocabAnalyzer>();

        return services;
    }
}
