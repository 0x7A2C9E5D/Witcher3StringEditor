using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Defines a contract for serialization and deserialization of The Witcher 3 string items
///     This interface provides the basic functionality for converting between The Witcher 3 string items and various file
///     formats
/// </summary>
public interface ISerializer
{
    /// <summary>
    ///     Determines whether this serializer reads and writes the given file format
    /// </summary>
    /// <param name="fileFormat">The file format to check</param>
    /// <returns>True when this serializer handles that format</returns>
    public bool CanHandle(FileFormat fileFormat);

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a file
    /// </summary>
    /// <param name="filePath">The path to the file to deserialize from</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation. The task result contains the deserialized W3
    ///     string items
    /// </returns>
    /// <exception cref="Exception">Thrown when the file could not be read</exception>
    public Task<IReadOnlyList<IStringItem>> Deserialize(string filePath);

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing file path and other serialization parameters</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation. The task result indicates whether the
    ///     serialization was successful
    /// </returns>
    public Task<bool> Serialize(IReadOnlyList<IStringItem> w3StringItems, SerializationContext context);
}