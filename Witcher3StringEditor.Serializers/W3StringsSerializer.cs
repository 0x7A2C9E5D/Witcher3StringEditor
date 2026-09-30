using System.Globalization;
using System.Text;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Model;
using Witcher3StringEditor.W3Strings;
using Witcher3StringEditor.W3Strings.Model;

namespace Witcher3StringEditor.Serializers;

/// <summary>
///     Provides W3Strings serialization functionality for The Witcher 3 string items
///     Implements IW3Serializer for the W3Strings container format
///     This serializer uses the built-in W3Strings codec, so no external encoder/decoder tool is required
/// </summary>
public class W3StringsSerializer(IBackupService backupService) : IW3Serializer
{
    /// <summary>
    ///     Determines whether this serializer reads and writes the given file format
    /// </summary>
    /// <param name="fileFormat">The file format to check</param>
    /// <returns>True when the format is a W3Strings container</returns>
    public bool CanHandle(W3FileFormat fileFormat)
    {
        return fileFormat == W3FileFormat.W3Strings;
    }

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a W3Strings file
    ///     Parses the container directly with the built-in codec, which covers both the UTF-16 and the UTF-8 generations
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items, or an empty list if an error occurred
    /// </returns>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath)
    {
        try
        {
            return await Task.Run(() => ReadItems(filePath)); // Decode the container off the calling thread
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deserializing W3Strings file: {Path}",
                filePath); // Log any errors that occur during deserialization
            return []; // Return an empty list in case of errors
        }
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
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        try
        {
            // The path is built first: every anomaly the container is checked for is logged with the
            // file it is about, which is the only way to tell two runs apart in the log.
            var saveLang =
                context.TargetLanguage.Code
                    .ToLowerInvariant(); // Lowercase content-file code of the target language for file naming
            var outputW3StringsPath =
                Path.Combine(context.OutputDirectory, $"{saveLang}.w3strings"); // Destination of the encoded container
            var logger = Log.ForContext("Container", outputW3StringsPath); // Every line names its container

            // The rows are checked before anything is written, so a save that cannot be written leaves
            // the destination exactly as it was. The names come first: two rows with one name are a
            // problem of the rows, which the container can no longer see once their hashes are built.
            var inputs = w3StringItems
                .Select(item => new Item(item.StrId, item.KeyName, item.KeyHex, item.Text)) // Raw, unparsed
                .ToList();
            var namesAreUsable = W3StringsValidator.ValidateKeyNames(inputs, logger);
            var parsed = W3StringsValidator.ValidateItems(inputs, logger);
            if (parsed is null || !namesAreUsable)
            {
                logger.Error("Refusing to write {Count} item(s): some of them carry no usable string ID or key",
                    w3StringItems.Count);
                return false;
            }

            var container = BuildContainer(parsed, context);
            if (!W3StringsValidator.Validate(container, logger))
            {
                // Writing it would produce a file whose entries cannot all be reached, so the reasons
                // above stand in place of a silently unsound mod.
                logger.Error("Refusing to write {Count} item(s): the container holds errors", w3StringItems.Count);
                return false;
            }

            // Back the destination up before it is opened, because creating it truncates it.
            if (File.Exists(outputW3StringsPath) &&
                !await backupService.Backup(outputW3StringsPath)) // Back the destination up before overwriting
                return false; // Leave the existing file alone when its backup could not be taken

            // The container is streamed straight into its destination, so the encoded file is never
            // held in memory: only one payload exists at a time.
            using (var stream = File.Create(outputW3StringsPath))
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
        return encoding.CodePage == Encoding.UTF8.CodePage
            ? W3StringsFormat.FirstUtf8Version
            : W3StringsFormat.Utf16LeVersion;
    }

    /// <summary>
    ///     Reads every string entry of a container into The Witcher 3 string items
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to read</param>
    /// <returns>The string items of the container, in container order</returns>
    /// <exception cref="W3StringsException">Thrown when the file is not a container this build can decode</exception>
    private static List<IW3StringItem> ReadItems(string filePath)
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

        // Block 2 maps a localization-key hash to the id it resolves to. An id can carry
        // several keys, so the first hash found is the one shown next to the entry.
        var keyHashes = new Dictionary<uint, uint>(container.Keys.Count);
        foreach (var key in container.Keys)
            keyHashes.TryAdd(key.Id, key.KeyHash);

        var items = new List<IW3StringItem>(container.Strings.Count); // Create list to store items
        items.AddRange(container.Strings.Select(entry => new W3StringItem // Create new string item
        {
            StrId = entry.Id.ToString(CultureInfo.InvariantCulture), // String id
            KeyHex = keyHashes.TryGetValue(entry.Id, out var keyHash)
                ? keyHash.ToString("X8", CultureInfo.InvariantCulture)
                : string.Empty, // Localisation key hash, when the entry has one
            Text = entry.Value // Decoded text
        }));
        return items; // Return list of items
    }

    /// <summary>
    ///     Builds a W3Strings container from the items that have already been checked
    /// </summary>
    /// <param name="items">The parsed items to include, in the order they were given</param>
    /// <param name="context">The serialization context supplying the target language and container version</param>
    /// <returns>The container to encode</returns>
    private static W3StringsFile BuildContainer(IReadOnlyList<Item> items, W3SerializationContext context)
    {
        var language = context.TargetLanguage; // Language the container is written for
        var container = new W3StringsFile
        {
            Version = ContainerVersion(context.Encoding), // Container version that stores the chosen encoding
            // The key is the only thing a container is told about its language: the magic that every id
            // and text is obfuscated with follows from it.
            Key = language.Key
        };

        foreach (var item in items)
        {
            container.Strings.Add(new W3StringEntry { Id = item.Id, Value = item.Text });
            if (item.KeyHash is { } hash)
                container.Keys.Add(new W3KeyEntry { KeyHash = hash, Id = item.Id });
        }

        return container;
    }
}