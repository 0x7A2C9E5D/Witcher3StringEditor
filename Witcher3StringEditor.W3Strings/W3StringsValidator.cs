using System.Globalization;
using Serilog;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Checks the container that is about to be written to a w3strings file
///     The check runs on the save path only. Reading a file never fails over content that still decodes,
///     and the other file formats keep their own semantics, so neither of them comes through here
/// </summary>
public static class W3StringsValidator
{
    /// <summary>
    ///     Checks the items that are about to be written, and answers what each of them holds
    ///     The rows are read before anything else happens, so a save that cannot be written is refused
    ///     before the destination file is touched
    /// </summary>
    /// <param name="items">The raw string id, key name and key hash of every item to write</param>
    /// <param name="logger">The logger every row that cannot be written is reported to</param>
    /// <returns>
    ///     The parsed items in the order they were given, or null when one of them cannot be written
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the items or the logger is null</exception>
    public static IReadOnlyList<Item>? ValidateItems(IReadOnlyList<Item> items, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(logger);

        // Every row is gone through even after one of them fails, so that all of them are reported at
        // once instead of one per attempt. All of them have to hold for the save to happen at all.
        var parsed = new List<Item>(items.Count);
        return items.All(item => TryParse(item, parsed, logger)) ? parsed : null;
    }

    /// <summary>
    ///     Reads the string id and the key hash of one item, and keeps it when it can be written
    /// </summary>
    /// <param name="item">The item to read</param>
    /// <param name="parsed">Receives the item with its string id and key hash read</param>
    /// <param name="logger">The logger a row that cannot be written is reported to</param>
    /// <returns>True when the item holds a string id and, if it carries a key, a usable key hash</returns>
    private static bool TryParse(Item item, List<Item> parsed, ILogger logger)
    {
        if (!uint.TryParse(item.StringId, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            logger.Error("Skipping an item whose string ID {StringId} is not a 32-bit unsigned integer",
                item.StringId);
            return false;
        }

        var (keyIsUsable, keyHash) = ResolveKeyHash(item, id, logger);
        if (!keyIsUsable) return false;

        parsed.Add(item with { Id = id, KeyHash = keyHash });
        return true;
    }

    /// <summary>
    ///     Resolves the localization-key hash of one item
    ///     The readable key name wins over the raw hash when both are set
    /// </summary>
    /// <param name="item">The item to resolve the key of</param>
    /// <param name="id">The parsed string id of the item, used to name the row</param>
    /// <param name="logger">The logger an unusable key hash is reported to</param>
    /// <returns>
    ///     Whether the key may be written, and the key hash itself. The hash is null when the item
    ///     carries no key at all, which is not an error
    /// </returns>
    private static (bool KeyIsUsable, uint? KeyHash) ResolveKeyHash(Item item, uint id, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            return (true, LocalizationKeyHash.Compute(item.KeyName)); // Hash the readable localization key
        if (string.IsNullOrWhiteSpace(item.KeyHex))
            return (true, null); // The item carries no key at all

        var keyHex = item.KeyHex.Trim().ToLowerInvariant();
        if (keyHex.StartsWith("0x", StringComparison.Ordinal)) keyHex = keyHex[2..]; // Drop an optional prefix
        if (uint.TryParse(keyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var keyHash))
            return (true, keyHash);

        logger.Error(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so the key is dropped",
            id, item.KeyHex);
        return (false, null);
    }

    /// <summary>
    ///     Checks a container that is about to be written and writes every anomaly to the log
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when the container may be written, false when one of its entries is unreachable</returns>
    /// <exception cref="ArgumentNullException">Thrown when the container or the logger is null</exception>
    public static bool Validate(W3StringsFile file, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(logger);

        if (file.Strings.Count == 0)
        {
            logger.Error("The container holds no string entry at all");
            return false;
        }

        var mayWrite = ValidateStrings(file, logger);
        if (!ValidateKeys(file, logger)) mayWrite = false;

        return mayWrite;
    }

    /// <summary>
    ///     Checks the readable key names of the items that are about to be written
    ///     Two items that carry the same name end up with the same key hash, so the container cannot tell
    ///     them apart anymore and one of the two texts becomes unreachable. Only a name collision can be
    ///     reported here, because the container itself no longer holds the names
    /// </summary>
    /// <param name="items">The items that are about to be written</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when every name belongs to a single string id, false when one of them is shared</returns>
    /// <exception cref="ArgumentNullException">Thrown when the items or the logger is null</exception>
    public static bool ValidateKeyNames(IEnumerable<Item> items, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(logger);

        var idsByKeyName = new Dictionary<string, HashSet<uint>>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.KeyName)) continue;
            if (!idsByKeyName.TryGetValue(item.KeyName, out var ids))
                idsByKeyName[item.KeyName] = ids = [];

            ids.Add(item.Id);
        }

        var mayWrite = true;
        foreach (var (keyName, ids) in idsByKeyName.Where(pair => pair.Value.Count > 1))
        {
            var relatedIds = ids.Order().ToArray();
            logger.Error(
                "The key name {KeyName} is used by {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                keyName, relatedIds.Length, string.Join(", ", relatedIds));
            mayWrite = false;
        }

        return mayWrite;
    }

    /// <summary>
    ///     Reports the anomalies of the string block
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when no string entry is unreachable</returns>
    private static bool ValidateStrings(W3StringsFile file, ILogger logger)
    {
        var mayWrite = true;

        // Two entries under one id means the game resolves the id to a single text while the other
        // one is silently unreachable, which is why such a container is refused.
        foreach (var group in file.Strings.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
        {
            logger.Error(
                "String ID {StringId} appears {Count} time(s) in the container, so only one of its texts can be reached",
                group.Key, group.Count());
            mayWrite = false;
        }

        foreach (var entry in file.Strings.Where(entry => entry.Value.Contains('\0')))
            logger.Warning("String ID {StringId} contains a null character, the game cuts the text there", entry.Id);

        var keyedIds = file.Keys.Select(key => key.Id).ToHashSet();
        var orphanCount = file.Strings.Select(entry => entry.Id).Distinct().Count(id => !keyedIds.Contains(id));
        if (orphanCount > 0)
            logger.Information("{Count} string(s) have no key and cannot be reached through a key lookup",
                orphanCount);

        return mayWrite;
    }

    /// <summary>
    ///     Reports the anomalies of the key block
    /// </summary>
    /// <param name="file">The container to check</param>
    /// <param name="logger">The logger the anomalies are written to</param>
    /// <returns>True when every key resolves to a single string entry</returns>
    private static bool ValidateKeys(W3StringsFile file, ILogger logger)
    {
        foreach (var group in file.Keys.GroupBy(key => (key.KeyHash, key.Id)).Where(group => group.Count() > 1))
            logger.Warning("Key hash 0x{KeyHash:X8} of string ID {StringId} is listed {Count} time(s)",
                group.Key.KeyHash, group.Key.Id, group.Count());

        var mayWrite = true;
        foreach (var group in file.Keys.GroupBy(key => key.KeyHash)
                     .Where(group => group.Select(key => key.Id).Distinct().Count() > 1))
        {
            var relatedIds = group.Select(key => key.Id).Distinct().Order().ToArray();
            logger.Error(
                "Key hash 0x{KeyHash:X8} points at {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                group.Key, relatedIds.Length, string.Join(", ", relatedIds));
            mayWrite = false;
        }

        foreach (var group in file.Keys.GroupBy(key => key.Id)
                     .Where(group => group.Select(key => key.KeyHash).Distinct().Count() > 1))
            logger.Warning("String ID {StringId} is referenced by {Count} different keys, only the first one is kept",
                group.Key, group.Select(key => key.KeyHash).Distinct().Count());

        return mayWrite;
    }
}

/// <summary>
///     One item that is about to be written, as it arrives and then as it is read
/// </summary>
/// <param name="StringId">The string id the item carries, still unparsed</param>
/// <param name="KeyName">The readable localization key of the item, which wins over the key hash</param>
/// <param name="KeyHex">The hexadecimal key hash of the item, used when the item carries no key name</param>
/// <param name="Text">The text of the item, which is written as it is</param>
/// <param name="Id">The parsed string id, once the item has been read</param>
/// <param name="KeyHash">The parsed key hash, null when the item carries no key</param>
public record Item(string StringId, string KeyName, string KeyHex, string Text, uint Id = 0, uint? KeyHash = null);