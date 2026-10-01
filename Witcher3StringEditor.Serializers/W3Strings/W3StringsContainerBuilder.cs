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
    /// <remarks>
    ///     The container is assembled in the order the format needs it: every item is read and indexed,
    ///     what that turned up is reported, the keys are laid down, and only then is the container judged.
    ///     A container that cannot be written is refused before the caller touches a file
    /// </remarks>
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key, ILogger logger)
    {
        var file = new W3StringsFile { Version = version, Key = key };

        // Every row is read even after one of them fails, so that all of them are reported at once
        // instead of one per attempt.
        var index = new Index();
        var mayWrite = ReadItems(items, file, index, logger);

        if (file.Strings.Count == 0) logger.Error("The container holds no string entry at all");

        // An id has to be unique, and so does the hash of a key: a container that repeats either of them
        // cannot be looked up unambiguously, so it is refused rather than written.
        ReportDuplicateIds(file, logger, ref mayWrite);
        ReportDuplicateKeys(index, logger, ref mayWrite);
        AddKeys(file, index);

        // The keys are laid down before the container is judged, because whether there are any is what
        // the game's own encoder cannot do without: it reads a container with an empty key block as a
        // broken file and gives up on it. Every item of a mod carries a key name or a key hash, which is
        // where the key block comes from.
        if (file.Strings.Count > 0 && file.Keys.Count == 0)
        {
            logger.Error(
                "None of the {Count} item(s) carries a key, and the game's encoder cannot read a container without a key block",
                file.Strings.Count);
            mayWrite = false;
        }

        if (!mayWrite || file.Strings.Count == 0)
        {
            logger.Error("Refusing to write {Count} item(s): the container holds errors", items.Count);
            return null;
        }

        return file;
    }

    /// <summary>
    ///     Reads every item into the container, and indexes the key it carries by hash and by name
    /// </summary>
    /// <param name="items">The items to read</param>
    /// <param name="file">The container the entries are added to</param>
    /// <param name="index">The index the keys of the items are collected in</param>
    /// <param name="logger">The logger an item that cannot be read is written to</param>
    /// <returns>True when every item may be written</returns>
    private static bool ReadItems(IReadOnlyList<IStringItem> items, W3StringsFile file, Index index, ILogger logger)
    {
        var mayWrite = true;
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

            file.Strings.Add(new W3StringEntry { Id = id, Value = item.Text });

            // A key hash of zero is how an item says it carries no key at all, so nothing is indexed for
            // it: its id stays in the container and is reached by id rather than by any key.
            if (keyHash is not { } hash || hash == 0) continue;
            index.Add(hash, id, item.KeyName);
        }

        return mayWrite;
    }

    /// <summary>
    ///     Reports every id that several entries share
    /// </summary>
    /// <param name="file">The container whose entries are reported on</param>
    /// <param name="logger">The logger every repeated id is written to</param>
    /// <param name="mayWrite">Set to false, because an id has to be unique</param>
    private static void ReportDuplicateIds(W3StringsFile file, ILogger logger, ref bool mayWrite)
    {
        foreach (var group in file.Strings.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
        {
            logger.Error(
                "String ID {StringId} is used by {Count} entries, so the container cannot be looked up",
                group.Key, group.Count());
            mayWrite = false;
        }
    }

    /// <summary>
    ///     Reports every hash and name that several entries share
    /// </summary>
    /// <param name="index">The index of the keys the entries carry</param>
    /// <param name="logger">The logger every repeated key is written to</param>
    /// <param name="mayWrite">Set to false, because a key has to be unique</param>
    private static void ReportDuplicateKeys(Index index, ILogger logger, ref bool mayWrite)
    {
        // One hash claimed by several ids: only one of them can ever be looked up by that key.
        foreach (var (hash, extraIds) in index.ExtraIdsByHash)
        {
            var ids = new[] { index.IdsByHash[hash] }.Concat(extraIds).Order().ToArray();
            var name = index.NamesByHash.TryGetValue(hash, out var named) ? $" (named {named})" : string.Empty;
            logger.Error(
                "Key hash 0x{Hash:X8}{Name} points at {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                hash, name, ids.Length, string.Join(", ", ids));
            mayWrite = false;
        }

        // The container keeps the hash of a key name, not the name, so two items carrying one name end up
        // sharing a hash while pointing at different ids.
        foreach (var (keyName, extraIds) in index.ExtraIdsByName)
        {
            var ids = new[] { index.IdsByName[keyName] }.Concat(extraIds).Order().ToArray();
            logger.Error(
                "The key name {KeyName} is used by {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                keyName, ids.Length, string.Join(", ", ids));
            mayWrite = false;
        }
    }

    /// <summary>
    ///     Lays the key block of the container down from the hashes its entries carry
    /// </summary>
    /// <param name="file">The container the keys are added to</param>
    /// <param name="index">The index of the hashes and the ids they point at</param>
    private static void AddKeys(W3StringsFile file, Index index)
    {
        foreach (var (hash, id) in index.IdsByHash)
            file.Keys.Add(new W3KeyEntry { KeyHash = hash, Id = id });
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
    private static bool TryResolveKey(IStringItem item, uint id, out uint? keyHash, ILogger logger)
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

    /// <summary>
    ///     The keys the items of a save carry, collected while they are read
    /// </summary>
    /// <remarks>
    ///     A key is a hash and nothing else: a container stores which hash resolves to which string id, and
    ///     a name is only ever an input that is hashed on the way in. Each of those hashes belongs to one
    ///     string id, so the two are paired one to one here, and a second id claiming a hash already in use
    ///     is collected rather than stored, because a container cannot hold it
    /// </remarks>
    private sealed class Index
    {
        /// <summary>
        ///     The string id each key hash points at
        /// </summary>
        public Dictionary<uint, uint> IdsByHash { get; } = [];

        /// <summary>
        ///     The string id each key name is used by, for the names that arrived as names
        /// </summary>
        public Dictionary<string, uint> IdsByName { get; } = new(StringComparer.Ordinal);

        /// <summary>
        ///     The key name behind each hash a name was hashed into, for a hash that arrived as a name
        /// </summary>
        public Dictionary<uint, string> NamesByHash { get; } = [];

        /// <summary>
        ///     The further string ids that claim a hash which already points at another one, in the order
        ///     they arrived: what a container cannot store, and what the caller is told about
        /// </summary>
        public Dictionary<uint, List<uint>> ExtraIdsByHash { get; } = [];

        /// <summary>
        ///     The further string ids that use a key name which already points at another one, in the order
        ///     they arrived: the same clash, seen through the name it was written with
        /// </summary>
        public Dictionary<string, List<uint>> ExtraIdsByName { get; } = new(StringComparer.Ordinal);

        /// <summary>
        ///     Indexes the key one entry carries
        /// </summary>
        /// <param name="hash">The hash of the key, which is never zero here</param>
        /// <param name="id">The string id the key points at</param>
        /// <param name="keyName">The name the hash was computed from, empty when it arrived as a hash</param>
        public void Add(uint hash, uint id, string keyName)
        {
            // A container keeps one id for a hash, so a second one cannot be looked up by that key.
            if (IdsByHash.TryGetValue(hash, out var firstId))
            {
                if (firstId != id)
                {
                    if (!ExtraIdsByHash.TryGetValue(hash, out var extraHash)) ExtraIdsByHash[hash] = extraHash = [];
                    extraHash.Add(id);
                }
            }
            else
            {
                IdsByHash[hash] = id;
            }

            if (string.IsNullOrWhiteSpace(keyName)) return;

            if (IdsByName.TryGetValue(keyName, out var firstIdForName))
            {
                if (firstIdForName != id)
                {
                    if (!ExtraIdsByName.TryGetValue(keyName, out var extraName))
                        ExtraIdsByName[keyName] = extraName = [];
                    extraName.Add(id);
                }
            }
            else
            {
                IdsByName[keyName] = id;
            }

            // The name is what a clash under this hash should be reported with.
            NamesByHash[hash] = keyName;
        }
    }
}