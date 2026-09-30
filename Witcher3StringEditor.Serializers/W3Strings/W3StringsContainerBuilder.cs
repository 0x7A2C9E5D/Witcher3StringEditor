using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Assembles a container out of the items that are about to be written
///     Every rule that turns an item into an entry of a container lives here: whether its string id can be
///     read, which of its two keys wins, and which of two items claiming the same key can still be looked
///     up. The caller is told whether the container may be written at all before it touches a file, and
///     never has to know any of those rules itself
/// </summary>
internal static class W3StringsContainerBuilder
{
    /// <summary>
    ///     Builds the container of the given items
    /// </summary>
    /// <param name="items">The items to write, as they arrive from the caller</param>
    /// <param name="version">The container version that stores the chosen payload encoding</param>
    /// <param name="key">The language key the container is written for</param>
    /// <param name="logger">The logger every anomaly is written to</param>
    /// <returns>The container to encode, or null when one of its entries would be unreachable</returns>
    public static W3StringsFile? Build(IReadOnlyList<IW3StringItem> items, uint version, uint key, ILogger logger)
    {
        var file = new W3StringsFile { Version = version, Key = key };

        // Every row is read even after one of them fails, so that all of them are reported at once
        // instead of one per attempt. What each of them contributes is counted as it is read, because two
        // entries under one id, or two keys hashing to one value, make one of the texts unreachable.
        var mayWrite = true;
        var nameIds = new Dictionary<string, HashSet<uint>>(StringComparer.Ordinal);
        var idsByHash = new Dictionary<uint, HashSet<uint>>();
        var namesByHash = new Dictionary<uint, string?>();

        foreach (var item in items)
        {
            if (!TryParseId(item.StrId, out var id))
            {
                logger.Error("Skipping an item whose string ID {StringId} is not a 32-bit unsigned integer",
                    item.StrId);
                mayWrite = false;
                continue;
            }

            if (!TryResolveKey(item, id, out var keyHash, logger)) mayWrite = false;

            // A second entry under one id is written as well: both texts are in the container, and the
            // game resolves the id to one of them, which is reported below.
            file.Strings.Add(new W3StringEntry { Id = id, Value = item.Text });

            if (keyHash is not { } hash) continue;
            if (!namesByHash.ContainsKey(hash)) namesByHash[hash] = item.KeyName;
            if (idsByHash.TryGetValue(hash, out var idsForHash)) idsForHash.Add(id);
            else idsByHash[hash] = [id];

            if (string.IsNullOrWhiteSpace(item.KeyName)) continue;
            if (nameIds.TryGetValue(item.KeyName, out var idsForName)) idsForName.Add(id);
            else nameIds[item.KeyName] = [id];
        }

        foreach (var group in file.Strings.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
            logger.Warning("String ID {StringId} appears {Count} time(s) in the container, all of them written",
                group.Key, group.Count());

        if (file.Strings.Count == 0) logger.Error("The container holds no string entry at all");

        // One hash pointing at several ids: only one of them can ever be looked up by that key.
        foreach (var (hash, relatedIds) in idsByHash.Where(pair => pair.Value.Count > 1))
        {
            var order = relatedIds.Order().ToArray();
            var name = namesByHash[hash] is { } named ? $" (named {named})" : string.Empty;
            logger.Error(
                "Key hash 0x{Hash:X8}{Name} points at {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                hash, name, order.Length, string.Join(", ", order));
            mayWrite = false;
        }

        // The container keeps the hash of a key name, not the name, so two items carrying one name end up
        // sharing a hash while pointing at different ids.
        foreach (var (keyName, relatedIds) in nameIds.Where(pair => pair.Value.Count > 1))
        {
            var order = relatedIds.Order().ToArray();
            logger.Error(
                "The key name {KeyName} is used by {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                keyName, order.Length, string.Join(", ", order));
            mayWrite = false;
        }

        if (!mayWrite || file.Strings.Count == 0)
        {
            logger.Error("Refusing to write {Count} item(s): the container holds errors", items.Count);
            return null;
        }

        foreach (var (hash, relatedIds) in idsByHash)
            file.Keys.Add(new W3KeyEntry { KeyHash = hash, Id = relatedIds.First() });

        return file;
    }

    /// <summary>
    ///     Parses the string id of one item
    /// </summary>
    /// <param name="strId">The string id the item carries</param>
    /// <param name="id">Receives the parsed string id</param>
    /// <returns>True when the id is a 32-bit unsigned integer</returns>
    private static bool TryParseId(string strId, out uint id)
    {
        return uint.TryParse(strId, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }

    /// <summary>
    ///     Resolves the localization key of one item
    ///     The readable key name wins over the raw hash when both are set
    /// </summary>
    /// <param name="item">The item to resolve the key of</param>
    /// <param name="id">The parsed string id of the item, used to name the row</param>
    /// <param name="keyHash">Receives the key hash, null when the item carries no key at all</param>
    /// <param name="logger">The logger an unusable key hash is written to</param>
    /// <returns>True when the key may be written</returns>
    private static bool TryResolveKey(IW3StringItem item, uint id, out uint? keyHash, ILogger logger)
    {
        keyHash = null;
        if (!string.IsNullOrWhiteSpace(item.KeyName))
        {
            keyHash = LocalizationKeyHash.Compute(item.KeyName); // Hash the readable localization key
            return true;
        }

        if (string.IsNullOrWhiteSpace(item.KeyHex)) return true; // The item carries no key at all

        var keyHex = item.KeyHex.Trim();
        if (keyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            keyHex = keyHex[2..]; // Drop an optional hexadecimal prefix
        if (uint.TryParse(keyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
        {
            keyHash = hash;
            return true;
        }

        logger.Error(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so the key is dropped",
            id, item.KeyHex);
        return false;
    }
}