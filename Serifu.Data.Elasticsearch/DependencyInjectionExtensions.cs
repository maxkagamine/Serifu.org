using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;
using Kagamine.Extensions.Collections;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

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
        static void ConfigureJsonSerializerOptions(JsonSerializerOptions options)
        {
            options.TypeInfoResolver = JsonSourceGenerationContext.Default;
            options.Converters.Add(new JsonBase64ValueArrayConverter<Alignment>());
        }

        var settings = new ElasticsearchClientSettings(
            new SingleNodePool(new Uri(serverUrl)),
            (_, settings) => new DefaultSourceSerializer(settings, ConfigureJsonSerializerOptions));

        settings.DefaultIndex("quotes");
        settings.ThrowExceptions();

#if DEBUG
        settings.EnableDebugMode();
#endif

        return settings;
    }
}
