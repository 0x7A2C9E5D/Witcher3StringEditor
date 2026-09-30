using System.Text.Json;
using System.Text.Json.Serialization;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     JSON converter for The Witcher 3 languages
///     Stores a language as its content-file code, for example "cn"
/// </summary>
public class LanguageJsonConverter : JsonConverter<Language>
{
    /// <summary>
    ///     Reads a language from its content-file code, its culture code or its name
    ///     Anything else falls back to English so that an outdated or hand-edited settings file
    ///     cannot stop the application from starting
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override Language Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.String
            ? Language.FromCode(reader.GetString()) ?? Language.Default
            : Language.Default;
    }

    /// <summary>
    ///     Writes a language as its content-file code
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(
        Utf8JsonWriter writer,
        Language value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Code);
    }
}