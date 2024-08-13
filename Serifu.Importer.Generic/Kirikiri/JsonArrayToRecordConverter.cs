using DotNext.Reflection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serifu.Importer.Generic.Kirikiri;

/// <summary>
/// Deserializes an array as a record's positional arguments.
/// </summary>
/// <typeparam name="T">The record type.</typeparam>
internal class JsonArrayToRecordConverter<T> : JsonConverter<T>
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
