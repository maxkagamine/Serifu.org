using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Serifu.Data.Elasticsearch;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSerifuElasticsearch(this IServiceCollection services, string serverUrl)
    {
        // TODO: Get ES server url from configuration
        services.AddSingleton<IElasticsearchClientSettings>(CreateElasticsearchSettings(serverUrl));
        services.AddSingleton<ElasticsearchClient>();

        return services;
    }

    private static ElasticsearchClientSettings CreateElasticsearchSettings(string serverUrl)
    {
        var settings = new ElasticsearchClientSettings(
            new SingleNodePool(new Uri(serverUrl)),
            (_, settings) => new SourceSerializer(settings));

        settings.DefaultIndex(QuotesIndex.Name);
        settings.ThrowExceptions();

#if DEBUG
        settings.EnableDebugMode();
#endif

        return settings;
    }
}
