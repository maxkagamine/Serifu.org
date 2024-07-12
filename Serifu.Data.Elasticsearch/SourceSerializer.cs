using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Kagamine.Extensions.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Data.Elasticsearch;

// Fast-path source generated serialization isn't supported for async, but the only time we'll be serializing quotes is
// when building the container, so we'll just use the source generator to speed up deserialization.

[JsonSerializable(typeof(Quote), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true,
    Converters = [
        typeof(JsonBase64ValueArrayConverter<Alignment>)
    ]
)]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{ }

internal class SourceSerializer : SystemTextJsonSerializer
{
    public SourceSerializer(IElasticsearchClientSettings settings) : base(settings)
    {
        Initialize();
    }

    protected override JsonSerializerOptions? CreateJsonSerializerOptions()
    {
        return JsonSourceGenerationContext.Default.Options;
    }
}
