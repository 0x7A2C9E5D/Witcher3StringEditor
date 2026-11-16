using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Serializers.Abstractions;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Defines a contract for the serialization facade the rest of the application depends on
///     It delegates every request to the iw3StringSerializer able to handle the requested file format, so callers
///     never need to know which file formats exist
/// </summary>
public interface IW3StringSerializerCoordinator : IW3StringFileSerializer;

/// <summary>
///     Coordinates serialization operations for The Witcher 3 string items across different file formats
///     Acts as a facade that delegates every request to the iw3StringSerializer able to handle the requested file format
/// </summary>
public class W3StringIw3StringSerializerCoordinator : IW3StringSerializerCoordinator
{
    /// <summary>
    ///     The serializers to delegate to, one per supported file format
    /// </summary>
    private readonly IW3StringFileSerializer[] serializers;

    /// <summary>
    ///     Initializes a new instance of the W3StringIw3StringSerializerCoordinator class
    /// </summary>
    /// <param name="serializers">The serializers to delegate to</param>
    public W3StringIw3StringSerializerCoordinator(IEnumerable<IW3StringFileSerializer> serializers)
    {
        this.serializers = [.. serializers];
    }

    /// <summary>
    ///     Determines whether one of the serializers reads and writes the given file format
    /// </summary>
    /// <param name="w3StringFileFormat">The file format to check</param>
    /// <returns>True when one of the serializers handles that format</returns>
    public bool CanHandle(W3StringFileFormat w3StringFileFormat)
    {
        return serializers.Any(serializer => serializer.CanHandle(w3StringFileFormat));
    }

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a file
    ///     Determines the appropriate iw3StringSerializer based on the file extension
    /// </summary>
    /// <param name="filePath">The path to the file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the file format is not supported</exception>
    /// <exception cref="Exception">Thrown when the file could not be read</exception>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath)
    {
        var items = await Resolve(FormatOf(filePath)).Deserialize(filePath);
        Log.Information("Deserialized {Count} item(s) from {Path}", items.Count, filePath);
        return items;
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a file
    ///     Determines the appropriate iw3StringSerializer based on the target file format of the options
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="options">The serialization options containing the target file format and other parameters</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the target file format is not supported</exception>
    /// <remarks>
    ///     The text of every item is saved into the item itself first, through the item's own
    ///     <see cref="IW3StringItem.Text" />: the iw3StringSerializer that writes it asks no questions about line breaks,
    ///     and a caller that holds its items sees what a save wrote
    /// </remarks>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3StringSerializationOptions options)
    {
        // The text a file holds is written back into its items before anything is written: a line break of a
        // text is not what a saved text carries it as, and the save is the place that has both the items and
        // the rule, so no iw3StringSerializer has to know about it. The items then show what a save wrote.
        foreach (var item in w3StringItems)
            item.Text = SavedTextFormat.NormalizeLineBreaks(item.Text);

        var succeeded = await Resolve(options.TargetW3StringFileFormat).Serialize(w3StringItems, options);
        if (succeeded)
            Log.Information("Serialized {Count} item(s) as {W3StringFileFormat} to {Directory}", w3StringItems.Count,
                options.TargetW3StringFileFormat, options.OutputDirectory);
        return succeeded;
    }

    /// <summary>
    ///     Gets the iw3StringSerializer that handles the given file format
    /// </summary>
    /// <param name="w3StringFileFormat">The file format to handle</param>
    /// <returns>The iw3StringSerializer able to read and write that format</returns>
    /// <exception cref="NotSupportedException">Thrown when no iw3StringSerializer handles the format</exception>
    private IW3StringFileSerializer Resolve(W3StringFileFormat w3StringFileFormat)
    {
        return serializers.FirstOrDefault(serializer => serializer.CanHandle(w3StringFileFormat))
               ?? throw new NotSupportedException($"File format not supported: {w3StringFileFormat}");
    }

    /// <summary>
    ///     Maps the extension of a file path to its file format
    /// </summary>
    /// <param name="filePath">The path to map</param>
    /// <returns>The file format of the path</returns>
    /// <exception cref="NotSupportedException">Thrown when the extension is not supported</exception>
    private static W3StringFileFormat FormatOf(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".csv" => W3StringFileFormat.Csv,
            ".xlsx" => W3StringFileFormat.Excel,
            ".w3strings" => W3StringFileFormat.W3Strings,
            _ => throw new NotSupportedException($"File format not supported: {filePath}")
        };
    }
}