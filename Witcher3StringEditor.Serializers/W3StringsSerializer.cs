using System.Globalization;
using System.Text;
using CommunityToolkit.Diagnostics;
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
    ///     Container version of the UTF-8 generation
    /// </summary>
    private const uint Utf8ContainerVersion = 164;

    /// <summary>
    ///     Container version of the classic UTF-16LE generation
    /// </summary>
    private const uint Utf16LeContainerVersion = 162;

    /// <summary>
    ///     Gets the container version that stores the given payload encoding
    /// </summary>
    /// <param name="encoding">The payload encoding to store</param>
    /// <returns>The container version of the UTF-8 generation, or the classic one for every other encoding</returns>
    private static uint ContainerVersion(Encoding encoding)
    {
        return encoding.CodePage == Encoding.UTF8.CodePage ? Utf8ContainerVersion : Utf16LeContainerVersion;
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
    ///     Builds the container with the built-in codec and writes it into the output directory
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">
    ///     The serialization context containing output directory, target language,
    ///     container version, and other serialization parameters
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        var tempDirectory =
            Directory.CreateTempSubdirectory().FullName; // Create a temporary directory for intermediate files

        try
        {
            var saveLang =
                context.TargetLanguage.Code
                    .ToLowerInvariant(); // Lowercase content-file code of the target language for file naming
            var outputW3StringsPath =
                Path.Combine(context.OutputDirectory, $"{saveLang}.w3strings"); // Destination of the encoded container
            var tempW3StringsPath =
                Path.Combine(tempDirectory, $"{saveLang}.w3strings"); // Intermediate container file

            // Encode the container off the calling thread, then swap it in with a backup of the destination
            await Task.Run(() => W3StringsWriter.WriteFile(BuildContainer(w3StringItems, context), tempW3StringsPath));
            Guard.IsTrue(await ReplaceFileWithBackup(tempW3StringsPath,
                outputW3StringsPath)); // Replace the destination file with backup if needed
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
        finally
        {
            Directory.Delete(tempDirectory, true); // Delete the temporary directory
            Log.Debug("Temporary directory deleted: {Directory}",
                tempDirectory); // Delete the temporary directory
        }
    }

    /// <summary>
    ///     Reads every string entry of a container into The Witcher 3 string items
    /// </summary>
    /// <param name="filePath">The path to the W3Strings file to read</param>
    /// <returns>The string items of the container, in container order</returns>
    private static List<IW3StringItem> ReadItems(string filePath)
    {
        var container = W3StringsReader.ReadFile(filePath); // Parse the container
        Log.Information("Read W3Strings v{Version} container ({Language}) from {Path}", container.Version,
            container.Language?.ToString() ?? "unknown", filePath); // Log the container facts, including the detected language

        // Block 2 maps a localization-key hash to the id it resolves to. An id can carry
        // several keys, so the first hash found is the one shown next to the entry.
        var keyHashes = new Dictionary<uint, uint>(container.Keys.Count);
        foreach (var key in container.Keys)
            keyHashes.TryAdd(key.Id, key.KeyHash);

        var items = new List<IW3StringItem>(container.Strings.Count); // Create list to store items
        foreach (var entry in container.Strings)
            items.Add(new W3StringItem // Create new string item
            {
                StrId = entry.Id.ToString(CultureInfo.InvariantCulture), // String id
                KeyHex = keyHashes.TryGetValue(entry.Id, out var keyHash)
                    ? keyHash.ToString("X8", CultureInfo.InvariantCulture)
                    : string.Empty, // Localisation key hash, when the entry has one
                Text = entry.Value // Decoded text
            });
        return items; // Return list of items
    }

    /// <summary>
    ///     Builds a W3Strings container from The Witcher 3 string items
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to include</param>
    /// <param name="context">The serialization context supplying the target language and container version</param>
    /// <returns>The container to encode</returns>
    private static W3StringsFile BuildContainer(IReadOnlyList<IW3StringItem> w3StringItems,
        W3SerializationContext context)
    {
        var language = context.TargetLanguage; // Language the container is written for
        var container = new W3StringsFile
        {
            Version = ContainerVersion(context.Encoding), // Container version that stores the chosen encoding
            Language = language, // Target language
            Magic = language.Magic, // Magic XORed into the stored ids
            Key1 = (ushort)(language.Key >> 16), // High half of the language key
            Key2 = (ushort)(language.Key & 0xFFFF) // Low half of the language key
        };

        foreach (var w3StringItem in w3StringItems) // Process each string item
        {
            var id = ParseId(w3StringItem.StrId); // Parse the string id
            container.Strings.Add(new W3StringEntry { Id = id, Value = w3StringItem.Text }); // Add the string
            if (ResolveKeyHash(w3StringItem) is { } keyHash)
                container.Keys.Add(new W3KeyEntry { KeyHash = keyHash, Id = id }); // Add the localization key
        }

        return container; // Return the container
    }

    /// <summary>
    ///     Parses the string id of The Witcher 3 string item
    /// </summary>
    /// <param name="strId">The string id to parse</param>
    /// <returns>The parsed string id</returns>
    /// <exception cref="W3StringsException">Thrown when the id is not a 32-bit unsigned integer</exception>
    private static uint ParseId(string strId) =>
        uint.TryParse(strId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new W3StringsException($"'{strId}' is not a valid W3Strings string id");

    /// <summary>
    ///     Resolves the localization-key hash of The Witcher 3 string item
    ///     The readable key name wins over the raw hash when both are set
    /// </summary>
    /// <param name="w3StringItem">The string item to resolve the key of</param>
    /// <returns>The key hash, or null when the item carries no key</returns>
    /// <exception cref="W3StringsException">Thrown when the hexadecimal key is not a 32-bit unsigned integer</exception>
    private static uint? ResolveKeyHash(IW3StringItem w3StringItem)
    {
        if (!string.IsNullOrWhiteSpace(w3StringItem.KeyName))
            return LocalizationKeyHash.Compute(w3StringItem.KeyName); // Hash the localization key
        if (string.IsNullOrWhiteSpace(w3StringItem.KeyHex))
            return null; // The item carries no key at all

        var keyHex = w3StringItem.KeyHex.Trim(); // Trim the hexadecimal key
        if (keyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            keyHex = keyHex[2..]; // Drop an optional hexadecimal prefix
        return uint.TryParse(keyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var keyHash)
            ? keyHash
            : throw new W3StringsException($"'{w3StringItem.KeyHex}' is not a valid localisation key hash");
    }

    /// <summary>
    ///     Replaces a destination file with a source file, creating a backup of the destination file if it exists
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file</param>
    /// <param name="destinationFilePath">The path to the destination file</param>
    /// <returns>True if the replacement was successful, false otherwise</returns>
    private async Task<bool> ReplaceFileWithBackup(string sourceFilePath, string destinationFilePath)
    {
        if (File.Exists(destinationFilePath) &&
            !await backupService.Backup(destinationFilePath)) // Backup existing file before overwrite
            return false; // Return false if backup creation failed
        File.Copy(sourceFilePath, destinationFilePath, true); // Copy with overwrite
        return true; // Return true to indicate successful replacement
    }
}
