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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Larian;

[JsonSerializable(typeof(LsjFile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    Converters = [typeof(LsjGuidConverter), typeof(LsjTranslatedStringConverter)]
)]
internal sealed partial class LsjSourceGenerationContext : JsonSerializerContext
{
    private sealed class LsjGuidConverter : JsonConverter<LsjGuid?>
    {
        public override LsjGuid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            LsjString str = JsonSerializer.Deserialize(ref reader, Default.LsjString);

            if (string.IsNullOrEmpty(str.Value))
            {
                return null;
            }

            return new LsjGuid(System.Guid.Parse(str.Value));
        }

        public override void Write(Utf8JsonWriter writer, LsjGuid? value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class LsjTranslatedStringConverter : JsonConverter<LsjTranslatedString?>
    {
        public override LsjTranslatedString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            LsjTranslatedString str = JsonSerializer.Deserialize(ref reader, Default.LsjTranslatedString);

            if (string.IsNullOrEmpty(str.Handle))
            {
                return null;
            }

            return str;
        }

        public override void Write(Utf8JsonWriter writer, LsjTranslatedString? value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
