using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Data.Elasticsearch;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuElasticsearch(this IServiceCollection services)
    {
        // TODO: ES server defaults to localhost:9200, should get from configuration
        services.AddSingleton<IElasticsearchClientSettings>(new ElasticsearchClientSettings()
#if DEBUG
            .EnableDebugMode()
#endif
            .DefaultIndex("quotes")
            .ThrowExceptions());

        services.AddSingleton<ElasticsearchClient>();

        return services;
    }
}
