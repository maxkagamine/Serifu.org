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

using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Kirikiri;

internal sealed class JsonScnTranslationsConverter : JsonConverterFactory
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

internal sealed class JsonScnTranslationsConverter<T>(ScnParserOptions scnOptions) : JsonConverter<ScnTranslations<T>>
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
