using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Larian;

[JsonSerializable(typeof(LsjFile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    Converters = [typeof(LsjGuidConverter), typeof(LsjTranslatedStringConverter)]
)]
internal partial class LsjSourceGenerationContext : JsonSerializerContext
{
    private class LsjGuidConverter : JsonConverter<LsjGuid?>
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

    private class LsjTranslatedStringConverter : JsonConverter<LsjTranslatedString?>
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
