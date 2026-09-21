using System.Text.Json;
using System.Text.Json.Serialization;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     JSON converter for interfaces
/// </summary>
/// <typeparam name="TInterface">The interface that is serialized and deserialized</typeparam>
/// <typeparam name="TImpl">The single implementation the interface payload is materialized as</typeparam>
/// <remarks>
///     The converter round-trips correctly only while exactly one implementation exists per interface, which holds
///     for <c>IRecentFileEntry</c> and <c>IBackupItem</c>. Payloads produced by another implementation are coerced
///     to <typeparamref name="TImpl" /> and any additional state is dropped
/// </remarks>
public class InterfaceJsonConverter<TInterface, TImpl> : JsonConverter<TInterface>
    where TInterface : class
    where TImpl : TInterface
{
    /// <summary>
    ///     Reads an interface from JSON
    /// </summary>
    /// <param name="reader">The reader positioned at the payload</param>
    /// <param name="typeToConvert">The interface type being converted</param>
    /// <param name="options">The serializer options with this converter removed for the nested call</param>
    /// <returns>The materialized implementation instance, or null when the payload is null</returns>
    public override TInterface? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        // The nested call uses a copy of the options without this converter: if the implementation resolves back to
        // the interface this converter would otherwise recurse indefinitely
        var innerOptions = CreateInnerOptions(options);
        return JsonSerializer.Deserialize<TImpl>(ref reader, innerOptions);
    }

    /// <summary>
    ///     Writes an interface to JSON
    /// </summary>
    /// <param name="writer">The writer to write to</param>
    /// <param name="value">The value to write, which may be null</param>
    /// <param name="options">The serializer options with this converter removed for the nested call</param>
    public override void Write(
        Utf8JsonWriter writer,
        TInterface? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), CreateInnerOptions(options));
    }

    /// <summary>
    ///     Creates serializer options without this converter so nested serialization cannot re-enter it
    /// </summary>
    /// <param name="options">The serializer options</param>
    /// <returns>A copy of the options without this converter</returns>
    private static JsonSerializerOptions CreateInnerOptions(JsonSerializerOptions options)
    {
        var innerOptions = new JsonSerializerOptions(options);
        for (var i = innerOptions.Converters.Count - 1; i >= 0; i--)
            if (innerOptions.Converters[i] is InterfaceJsonConverter<TInterface, TImpl>)
                innerOptions.Converters.RemoveAt(i);
        return innerOptions;
    }
}