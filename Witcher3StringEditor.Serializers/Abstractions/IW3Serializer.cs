using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Defines a contract for serialization and deserialization of The Witcher 3 string items
///     This interface provides the basic functionality for converting between The Witcher 3 string items and various file
///     formats
/// </summary>
public interface IW3Serializer
{
    /// <summary>
    ///     Deserializes The Witcher 3 string items from a file
    /// </summary>
    /// <param name="filePath">The path to the file to deserialize from</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation. The task result contains the deserialized W3
    ///     string items
    /// </returns>
    public Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath);

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing file path and other serialization parameters</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation. The task result indicates whether the
    ///     serialization was successful
    /// </returns>
    public Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context);
}