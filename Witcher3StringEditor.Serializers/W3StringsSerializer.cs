using System.Text;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.W3Strings;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Provides W3Strings serialization functionality for The Witcher 3 string items
///     Implements ISerializer for the W3Strings container format
///     This serializer uses the built-in W3Strings codec, so no external encoder/decoder tool is required
/// </summary>
public class W3StringsSerializer(IBackupService backupService) : ISerializer
{
    /// <summary>
    ///     Determines whether this serializer reads and writes the given file format
    /// </summary>
    /// <param name="fileFormat">The file format to check</param>
    /// <returns>True when the format is a W3Strings container</returns>
    public bool CanHandle(FileFormat fileFormat)
    {
        return fileFormat == FileFormat.W3Strings;
    }

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a W3Strings file
    ///     Parses the container directly with the built-in codec, which covers both the UTF-16 and the UTF-8 generations
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items
    /// </returns>
    /// <exception cref="Exception">Thrown when the file could not be read</exception>
    public async Task<IReadOnlyList<IStringItem>> Deserialize(string filePath)
    {
        return await Task.Run(() => ReadItems(filePath)); // Decode the container off the calling thread
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a W3Strings file
    ///     Builds the container with the built-in codec, checks it, and writes it into the output directory
    ///     This is the only place the content is checked: the other serializers write their formats as they
    ///     always did, and reading a file only decodes it
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">
    ///     The serialization context containing output directory, target language,
    ///     container version, and other serialization parameters
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful, which it is not when the
    ///     container holds an entry the game could never reach
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IStringItem> w3StringItems, SerializationContext context)
    {
        try
        {
            var saveLang =
                context.TargetLanguage.Code
                    .ToLowerInvariant(); // Lowercase content-file code of the target language for file naming
            var outputW3StringsPath =
                Path.Combine(context.OutputDirectory, $"{saveLang}.w3strings"); // Destination of the encoded container

            // The container is assembled and checked by the codec before anything is written, so a save
            // that cannot be written leaves the destination exactly as it was. What makes an item
            // unusable is a rule of the format, so it is not decided here. The builder logs every
            // anomaly it refuses the container for itself, one line per anomaly.
            var container = W3StringsBuilder.Build(
                w3StringItems,
                ContainerVersion(context.Encoding), // Container version that stores the chosen encoding
                context.TargetLanguage.Key); // The language key is what every id and text is obfuscated by
            if (container is null) return false; // The reasons are in the log, one per anomaly

            // Back the destination up before it is opened, because creating it truncates it.
            if (File.Exists(outputW3StringsPath) &&
                !await backupService.Backup(outputW3StringsPath)) // Back the destination up before overwriting
                return false; // Leave the existing file alone when its backup could not be taken

            // The container is streamed straight into its destination, so the encoded file is never
            // held in memory: only one stored text exists at a time.
            await using (var stream = File.Create(outputW3StringsPath))
            {
                await Task.Run(() => W3StringsWriter.Write(stream, container)); // Encode off the calling thread
            }

            Log.Information("Encoded {Count} item(s) as W3Strings v{Version} to {Path}", w3StringItems.Count,
                ContainerVersion(context.Encoding), outputW3StringsPath); // Log the encoded container
            return true; // Return true to indicate successful serialization
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "An error occurred while serializing W3Strings"); // Log any errors that occur during serialization
            return false; // Return false to indicate serialization failure
        }
    }

    /// <summary>
    ///     Gets the container version that stores the given payload encoding
    /// </summary>
    /// <param name="encoding">The payload encoding to store</param>
    /// <returns>
    ///     The version of the UTF-8 generation, or the classic UTF-16LE one for every other encoding
    /// </returns>
    private static uint ContainerVersion(Encoding encoding)
    {
        return encoding.Equals(Encoding.UTF8) ? W3StringsFormat.FirstUtf8Version : W3StringsFormat.Utf16LeVersion;
    }

    /// <summary>
    ///     Reads every string entry of a container into The Witcher 3 string items
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to read</param>
    /// <returns>The string items of the container, in container order</returns>
    private static List<IStringItem> ReadItems(string filePath)
    {
        using var stream = File.OpenRead(filePath); // The codec reads a stream, the serializer knows the path
        var container = W3StringsReader.Read(stream); // Parse the container

        // The magic is all the container says about the language it was written for, and it is what the
        // ids and texts were decoded with.
        Log.Information("Read W3Strings v{Version} container (magic {Magic}) from {Path}", container.Version,
            container.Magic == 0 ? "none" : $"0x{container.Magic:X8}",
            filePath); // Log the container facts

        // Reading only decodes: what the container says about itself is not checked here, the check
        // belongs to the save that would produce a w3strings file again.
        return [.. W3StringsReader.ReadItems(container)];
    }
}