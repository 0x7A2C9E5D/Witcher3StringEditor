using System.Text.Json;
using System.Text.Json.Serialization;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///    JSON converter for interfaces
/// </summary>
/// <typeparam name="TInterface"></typeparam>
/// <typeparam name="TImpl"></typeparam>
public class InterfaceJsonConverter<TInterface, TImpl> : JsonConverter<TInterface>
    where TImpl : TInterface
{
    /// <summary>
    ///    Reads an interface from JSON
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override TInterface? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImpl>(ref reader, options);
    }

    /// <summary>
    ///    Writes an interface to JSON
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(
        Utf8JsonWriter writer,
        TInterface value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value!.GetType(), options);
    }
}