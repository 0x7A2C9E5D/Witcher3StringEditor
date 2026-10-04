using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     JSON converter for text encodings
///     Stores an encoding as its web name, for example "utf-8"
/// </summary>
public class EncodingJsonConverter : JsonConverter<Encoding>
{
    /// <summary>
    ///     Reads a text encoding from its web name
    ///     Anything that is not UTF-8 falls back to UTF-16LE, the default container encoding, so that
    ///     an outdated or hand-edited settings file cannot stop the application from starting
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override Encoding Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var webName = reader.GetString();
        return string.Equals(webName, Encoding.UTF8.WebName, StringComparison.OrdinalIgnoreCase)
            ? Encoding.UTF8
            : Encoding.Unicode;
    }

    /// <summary>
    ///     Writes a text encoding as its web name
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(
        Utf8JsonWriter writer,
        Encoding value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.WebName);
    }
}