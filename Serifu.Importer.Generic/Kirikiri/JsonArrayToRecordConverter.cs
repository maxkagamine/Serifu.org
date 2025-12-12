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

using DotNext.Reflection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Kirikiri;

/// <summary>
/// Deserializes an array as a record's positional arguments.
/// </summary>
/// <typeparam name="T">The record type.</typeparam>
internal sealed class JsonArrayToRecordConverter<T> : JsonConverter<T>
{
    private readonly ConstructorInfo ctor;
    private readonly Type[] ctorParams;

    public JsonArrayToRecordConverter()
    {
        ctor = typeof(T).GetConstructors().Single();
        ctorParams = ctor.GetParameterTypes();
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        List<object?> args = new(ctorParams.Length);
        reader.Read();

        while (reader.TokenType is not JsonTokenType.EndArray)
        {
            if (args.Count < ctorParams.Length)
            {
                Type nextType = ctorParams[args.Count];
                var arg = JsonSerializer.Deserialize(ref reader, nextType, options);
                args.Add(arg);
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }

            reader.Read();
        }

        if (args.Count != ctorParams.Length)
        {
            throw new JsonException($"Array did not contain the expected number of elements for the {typeof(T).Name} constructor.");
        }

        var obj = (T)ctor.Invoke([.. args]);
        return obj;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
