using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Coordinates serialization operations for The Witcher 3 string items across different file formats
///     Acts as a facade that delegates serialization requests to the appropriate specific serializer
///     based on file extension (for deserialization) or target file type (for serialization)
/// </summary>
public class W3SerializerCoordinator(
    ICsvW3Serializer csvW3Serializer,
    IExcelW3Serializer excelW3Serializer,
    IW3StringsSerializer w3StringsSerializer) : IW3Serializer
{
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
        // Determine the appropriate deserializer based on the file extension
        // Convert the extension to lowercase for case-insensitive comparison
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            // For CSV files, use the CSV serializer
            ".csv" => await csvW3Serializer.Deserialize(filePath),
            // For Excel files, use the Excel serializer
            ".xlsx" => await excelW3Serializer.Deserialize(filePath),
            // For W3Strings files, use the W3Strings serializer
            ".w3strings" => await w3StringsSerializer.Deserialize(filePath),
            // Throw an exception for unsupported file formats
            _ => throw new NotSupportedException($"File format not supported: {filePath}")
        };
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    ///     Determines the appropriate serializer based on the target file type specified in the context
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing the target file type and other parameters</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the target file type is not supported</exception>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        // Determine the appropriate serializer based on the target file type
        return context.TargetFileType switch
        {
            // For CSV file type, use the CSV serializer
            W3FileType.Csv => await csvW3Serializer.Serialize(w3StringItems, context),
            // For Excel file type, use the Excel serializer
            W3FileType.Excel => await excelW3Serializer.Serialize(w3StringItems, context),
            // For W3Strings file type, use the W3Strings serializer
            W3FileType.W3Strings => await w3StringsSerializer.Serialize(w3StringItems, context),
            // Throw an exception for unsupported file types
            _ => throw new NotSupportedException($"File type not supported: {context.TargetFileType}")
        };
    }
}