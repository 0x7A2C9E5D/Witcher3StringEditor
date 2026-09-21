using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.Abstractions;

/// <summary>
///     Defines a contract for serialization and deserialization of The Witcher 3 string items
///     This interface provides the basic functionality for converting between The Witcher 3 string items and various file
///     formats
/// </summary>
/// <remarks>
///     Failure semantics: deserialization throws when the file cannot be read or parsed, while serialization
///     returns <c>false</c> when to write itself failed (the cause is always logged) and throws when the request
///     itself was invalid. Implementations accept a null <c>w3StringItems</c> only where documented; a null,
///     empty or white-space <c>filePath</c> always throws <see cref="ArgumentException" />
/// </remarks>
public interface IW3Serializer
{
    /// <summary>
    ///     Deserializes The Witcher 3 string items from a file
    /// </summary>
    /// <param name="filePath">The path to the file to deserialize from</param>
    /// <param name="cancellationToken">A token used to abort the read</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation. The task result contains the deserialized W3
    ///     string items
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    /// <exception cref="NotSupportedException">The file format is not supported</exception>
    /// <exception cref="System.IO.IOException">The file could not be read</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled</exception>
    Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing file path and other serialization parameters</param>
    /// <param name="cancellationToken">A token used to abort to write</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation. The task result indicates whether the
    ///     serialization was successful
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="w3StringItems" /> or <paramref name="context" /> is null
    /// </exception>
    /// <exception cref="NotSupportedException">The target file type is not supported</exception>
    /// <exception cref="System.IO.IOException">The target file could not be written</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled</exception>
    Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context,
        CancellationToken cancellationToken = default);
}