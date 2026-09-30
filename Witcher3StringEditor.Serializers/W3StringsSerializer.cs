using System.Globalization;
using System.Text;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Model;
using Witcher3StringEditor.W3Strings;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

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
            var saveLang = context.TargetLanguage.Code.ToLowerInvariant(); // Lowercase content-file code of the target language for file naming
            var outputW3StringsPath =
                Path.Combine(context.OutputDirectory, $"{saveLang}.w3strings"); // Destination of the encoded container
            var logger = Log.ForContext("Container", outputW3StringsPath); // Every line names its container

            if (BuildContainer(w3StringItems, context, logger) is not { } container)
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
                await Task.Run(() => W3StringsWriter.Write(stream, container)); // Encode off the calling thread

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

        Log.Information("Read W3Strings v{Version} container ({Language}) from {Path}", container.Version,
            container.Language?.ToString() ?? "unknown",
            filePath); // Log the container facts, including the detected language

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
    ///     Builds a W3Strings container from The Witcher 3 string items, checking it on the way
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to include</param>
    /// <param name="context">The serialization context supplying the target language and container version</param>
    /// <param name="logger">The logger every anomaly of the built container is written to</param>
    /// <returns>The container to encode, or null when one of its entries would be unreachable</returns>
    private static W3StringsFile? BuildContainer(IReadOnlyList<IW3StringItem> w3StringItems,
        W3SerializationContext context, ILogger logger)
    {
        var language = context.TargetLanguage; // Language the container is written for
        var container = new W3StringsFile
        {
            Version = ContainerVersion(context.Encoding), // Container version that stores the chosen encoding
            Language = language, // Target language
            Magic = language.Magic, // Magic XORed into the stored ids
            Key = language.Key // Language key, stored split over the header and the end of the container
        };

        var mayWrite = true;
        var keyNames = new List<(uint Id, string KeyName)>(w3StringItems.Count);

        foreach (var w3StringItem in w3StringItems) // Process each string item
        {
            // An unreadable id used to abort the whole save with a bare exception. It is a property
            // of a single row, so the row is named instead and the user can find it again.
            if (!TryParseId(w3StringItem.StrId, out var id))
            {
                logger.Error("Skipping an item whose string ID {StringId} is not a 32-bit unsigned integer",
                    w3StringItem.StrId);
                mayWrite = false;
                continue; // Drop the row: nothing can be written for it anyway
            }

            container.Strings.Add(new W3StringEntry { Id = id, Value = w3StringItem.Text }); // Add the string

            var (keyIsUsable, keyHash) = ResolveKeyHash(w3StringItem, id, logger);
            if (!keyIsUsable) mayWrite = false;
            else if (keyHash is { } hash)
                container.Keys.Add(new W3KeyEntry { KeyHash = hash, Id = id }); // Add the localization key

            if (!string.IsNullOrWhiteSpace(w3StringItem.KeyName))
                keyNames.Add((id, w3StringItem.KeyName)); // Remember the name the hash was computed from
        }

        // Two items that share a key name end up sharing a key hash, which the container itself can no
        // longer tell apart from any other hash collision, so the names are checked separately.
        if (keyNames.Count > 0 && !W3StringsValidator.ValidateKeyNames(keyNames, logger)) mayWrite = false;

        // A container whose rows were all dropped as invalid is empty only as a consequence of the
        // rows already reported, so its emptiness is not stated a second time.
        var holdsARowOfItsOwn = container.Strings.Count > 0 || w3StringItems.Count == 0;
        if (holdsARowOfItsOwn && !W3StringsValidator.Validate(container, logger)) mayWrite = false;

        return mayWrite ? container : null; // Return the container, or null when it must not be written
    }

    /// <summary>
    ///     Parses the string id of The Witcher 3 string item
    /// </summary>
    /// <param name="strId">The string id to parse</param>
    /// <param name="id">Receives the parsed string id</param>
    /// <returns>True when the id is a 32-bit unsigned integer</returns>
    private static bool TryParseId(string? strId, out uint id)
    {
        return uint.TryParse(strId, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }

    /// <summary>
    ///     Resolves the localization-key hash of The Witcher 3 string item
    ///     The readable key name wins over the raw hash when both are set
    /// </summary>
    /// <param name="w3StringItem">The string item to resolve the key of</param>
    /// <param name="id">The already parsed string id of the item, used to name the row</param>
    /// <param name="logger">The logger an unusable key hash is written to</param>
    /// <returns>
    ///     Whether the key may be written, and the key hash itself. The hash is null when the item
    ///     carries no key at all, which is not an error
    /// </returns>
    private static (bool KeyIsUsable, uint? KeyHash) ResolveKeyHash(IW3StringItem w3StringItem, uint id,
        ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(w3StringItem.KeyName))
            return (true, LocalizationKeyHash.Compute(w3StringItem.KeyName)); // Hash the localization key
        if (string.IsNullOrWhiteSpace(w3StringItem.KeyHex))
            return (true, null); // The item carries no key at all

        var keyHex = w3StringItem.KeyHex.Trim(); // Trim the hexadecimal key
        if (keyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            keyHex = keyHex[2..]; // Drop an optional hexadecimal prefix
        if (uint.TryParse(keyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var keyHash))
            return (true, keyHash);

        logger.Error(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so the key is dropped",
            id, w3StringItem.KeyHex);
        return (false, null);
    }
}
