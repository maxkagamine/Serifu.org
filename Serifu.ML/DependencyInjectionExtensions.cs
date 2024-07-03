using Microsoft.Extensions.DependencyInjection;
using Serifu.ML.Abstractions;

namespace Serifu.ML;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuMachineLearning(this IServiceCollection services)
    {
        services.AddSingleton<ITransformersContext, TransformersContext>();
        services.AddSingleton<IWordAligner, WordAligner>();

        return services;
    }
}
