using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     JSON converter for ObservableCollection
/// </summary>
/// <typeparam name="T">The element type of the collection</typeparam>
/// <remarks>
///     A JSON <c>null</c> payload produces an empty collection instead of throwing, so a hand-edited settings file
///     cannot abort deserialization
/// </remarks>
public class ObservableCollectionJsonConverter<T> : JsonConverter<ObservableCollection<T>>
{
    /// <summary>
    ///     Reads an ObservableCollection from JSON
    /// </summary>
    /// <param name="reader">The reader positioned at the collection payload</param>
    /// <param name="typeToConvert">The type being converted</param>
    /// <param name="options">The serializer options</param>
    /// <returns>The deserialized collection, empty when the payload was null</returns>
    public override ObservableCollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        return list is null ? [] : new ObservableCollection<T>(list);
    }

    /// <summary>
    ///     Writes an ObservableCollection to JSON
    /// </summary>
    /// <param name="writer">The writer to write to</param>
    /// <param name="value">The collection to write</param>
    /// <param name="options">The serializer options</param>
    public override void Write(Utf8JsonWriter writer, ObservableCollection<T> value, JsonSerializerOptions options)
    {
        // The array is written element by element: delegating with IEnumerable<T> would re-resolve the collection
        // type against the options and could pick up an unrelated converter
        writer.WriteStartArray();
        foreach (var item in value) 
            JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}