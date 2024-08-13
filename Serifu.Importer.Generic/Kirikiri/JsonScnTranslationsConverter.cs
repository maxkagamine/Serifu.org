using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Kirikiri;

internal class JsonScnTranslationsConverter : JsonConverterFactory
{
    private readonly ScnParserOptions scnOptions;

    public JsonScnTranslationsConverter(IOptions<ScnParserOptions> options)
    {
        scnOptions = options.Value;
    }

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(ScnTranslations<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type t = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(JsonScnTranslationsConverter<>).MakeGenericType(t),
            scnOptions)!;
    }
}

internal class JsonScnTranslationsConverter<T>(ScnParserOptions scnOptions) : JsonConverter<ScnTranslations<T>>
{
    public override ScnTranslations<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String or JsonTokenType.Null && typeof(T) == typeof(string))
        {
            var str = (T)(object)(reader.GetString() ?? "");
            return new(str, str);
        }

        T[]? arr = JsonSerializer.Deserialize<T[]>(ref reader, options);

        if (arr is null || arr.Length - 1 < Math.Max(scnOptions.EnglishLanguageIndex, scnOptions.JapaneseLanguageIndex))
        {
            throw new JsonException();
        }

        T english = arr[scnOptions.EnglishLanguageIndex];
        T japanese = arr[scnOptions.JapaneseLanguageIndex];

        return new(english, japanese);
    }

    public override void Write(Utf8JsonWriter writer, ScnTranslations<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
