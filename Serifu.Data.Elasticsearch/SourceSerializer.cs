// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

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
