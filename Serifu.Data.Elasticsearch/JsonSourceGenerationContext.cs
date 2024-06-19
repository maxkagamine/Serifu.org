using System.Text.Json.Serialization;

namespace Serifu.Data.Elasticsearch;

// Fast-path source generated serialization isn't supported for async, but the only time we'll be serializing quotes is
// when building the container, so we'll just use the source generator to speed up deserialization.

[JsonSerializable(typeof(Quote), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class JsonSourceGenerationContext : JsonSerializerContext
{ }
