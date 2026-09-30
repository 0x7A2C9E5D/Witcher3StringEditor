using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Model;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Coordinates serialization operations for The Witcher 3 string items across different file formats
///     Acts as a facade that delegates every request to the serializer able to handle the requested file format
/// </summary>
public class W3SerializerCoordinator : IW3SerializerCoordinator
{
    /// <summary>
    ///     The serializers to delegate to, one per supported file format
    /// </summary>
    private readonly IW3Serializer[] serializers;

    /// <summary>
    ///     Initializes a new instance of the W3SerializerCoordinator class
    /// </summary>
    /// <param name="serializers">The serializers to delegate to</param>
    public W3SerializerCoordinator(IEnumerable<IW3Serializer> serializers)
    {
        this.serializers = [.. serializers];
    }

    /// <summary>
    ///     Determines whether one of the serializers reads and writes the given file format
    /// </summary>
    /// <param name="fileFormat">The file format to check</param>
    /// <returns>True when one of the serializers handles that format</returns>
    public bool CanHandle(W3FileFormat fileFormat)
    {
        return serializers.Any(serializer => serializer.CanHandle(fileFormat));
    }

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a file
    ///     Determines the appropriate serializer based on the file extension
    /// </summary>
    /// <param name="filePath">The path to the file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the file format is not supported</exception>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath)
    {
        var items = await Resolve(FormatOf(filePath)).Deserialize(filePath);
        Log.Information("Deserialized {Count} item(s) from {Path}", items.Count, filePath);
        return items;
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    ///     Determines the appropriate serializer based on the target file format of the context
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing the target file format and other parameters</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the target file format is not supported</exception>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        var succeeded = await Resolve(context.TargetFileFormat).Serialize(w3StringItems, context);
        if (succeeded)
            Log.Information("Serialized {Count} item(s) as {FileFormat} to {Directory}", w3StringItems.Count,
                context.TargetFileFormat, context.OutputDirectory);
        return succeeded;
    }

    /// <summary>
    ///     Gets the serializer that handles the given file format
    /// </summary>
    /// <param name="fileFormat">The file format to handle</param>
    /// <returns>The serializer able to read and write that format</returns>
    /// <exception cref="NotSupportedException">Thrown when no serializer handles the format</exception>
    private IW3Serializer Resolve(W3FileFormat fileFormat)
    {
        return serializers.FirstOrDefault(serializer => serializer.CanHandle(fileFormat))
               ?? throw new NotSupportedException($"File format not supported: {fileFormat}");
    }

    /// <summary>
    ///     Maps the extension of a file path to its file format
    /// </summary>
    /// <param name="filePath">The path to map</param>
    /// <returns>The file format of the path</returns>
    /// <exception cref="NotSupportedException">Thrown when the extension is not supported</exception>
    private static W3FileFormat FormatOf(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".csv" => W3FileFormat.Csv,
            ".xlsx" => W3FileFormat.Excel,
            ".w3strings" => W3FileFormat.W3Strings,
            _ => throw new NotSupportedException($"File format not supported: {filePath}")
        };
    }
}