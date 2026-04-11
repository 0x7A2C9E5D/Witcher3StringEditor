using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///    JSON converter for ObservableCollection
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObservableCollectionJsonConverter<T> : JsonConverter<ObservableCollection<T>>
{
    /// <summary>
    ///    Reads an ObservableCollection from JSON
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override ObservableCollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        return new ObservableCollection<T>(list!);
    }

    /// <summary>
    ///    Writes an ObservableCollection to JSON
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, ObservableCollection<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.AsEnumerable(), options);
    }
}