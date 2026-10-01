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
///     Every anomaly is written to the static Serilog logger, which the application configures once at
///     startup, so no logger is handed in here
///     An item is the caller's own object, so it is left carrying the key it is written with: the eight
///     digits of the hash, or no text at all when it carries no key. A name that is set is the key, and the
///     hash of it is put back on the item the moment the key is resolved, which is how the caller reads the
///     written key off the very item it handed in
/// </summary>
internal static class W3StringsContainerBuilder
{
    /// <summary>
    ///     Builds the container of the given items
    /// </summary>
    /// <param name="items">
    ///     The items to write, as they arrive from the caller. Every one of them is left carrying the key it
    ///     is written with, because they are the caller's own objects
    /// </param>
    /// <param name="version">The container version that stores the chosen payload encoding</param>
    /// <param name="key">The language key the container is written for</param>
    /// <returns>The container to encode, or null when one of its entries would be unreachable</returns>
    /// <remarks>
    ///     The container is assembled in the order the format needs it: every item is read and its key is
    ///     collected, what that turned up is reported, the keys are laid down, and only then is the
    ///     container judged. A container that cannot be written is refused before the caller touches a file
    /// </remarks>
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key)
    {
        // Nothing to assemble and nothing to report item by item: an empty save is refused here rather
        // than walked through the rules below.
        if (items.Count == 0)
        {
            Log.Error("The container holds no string entry at all");
            Log.Error("Refusing to write 0 item(s): the container holds errors");
            return null;
        }

        var file = new W3StringsFile { Version = version, Key = key };

        // The keys the items carry as they arrived: the hash, the string id it points at, and the name it
        // was computed from. A name is only input that gets hashed on the way in, so it is kept here and
        // not in the container. Every row is read even after one of them fails, so that all of them are
        // reported at once instead of one per attempt.
        List<(uint Hash, uint Id, string KeyName)> keys = [];
        var mayWrite = ReadItems(items, file, keys);

        // Every item was read but none of them could be turned into an entry.
        if (file.Strings.Count == 0)
        {
            Log.Error("The container holds no string entry at all");
            Log.Error("Refusing to write {Count} item(s): the container holds errors", items.Count);
            return null;
        }

        // An id has to be unique, and so does the hash of a key: a container that repeats either of them
        // cannot be looked up unambiguously, so it is refused rather than written.
        ReportDuplicateIds(file, ref mayWrite);
        ReportDuplicateKeys(keys, ref mayWrite);
        AddKeys(file, keys);

        // The keys are laid down before the container is judged, because whether there are any is what
        // the game's own encoder cannot do without: it reads a container with an empty key block as a
        // broken file and gives up on it. Every item of a mod carries a key name or a key hash, which is
        // where the key block comes from.
        if (file.Keys.Count == 0)
        {
            Log.Error(
                "None of the {Count} item(s) carries a key, and the game's encoder cannot read a container without a key block",
                file.Strings.Count);
            mayWrite = false;
        }

        if (mayWrite) return file;

        Log.Error("Refusing to write {Count} item(s): the container holds errors", items.Count);
        return null;
    }

    /// <summary>
    ///     Reads every item into the container, and collects the key it carries
    /// </summary>
    /// <param name="items">The items to read</param>
    /// <param name="file">The container the entries are added to</param>
    /// <param name="keys">The keys of the items are collected in</param>
    /// <returns>True when every item may be written</returns>
    private static bool ReadItems(IReadOnlyList<IStringItem> items, W3StringsFile file,
        List<(uint Hash, uint Id, string KeyName)> keys)
    {
        var mayWrite = true;
        foreach (var item in items)
        {
            if (!TryParseId(item.StrId, out var id))
            {
                Log.Error("Skipping an item whose string ID {StringId} is not a 32-bit unsigned integer",
                    item.StrId);
                mayWrite = false;
                continue;
            }

            // The key is resolved on the item itself, which is left carrying the key it is written with.
            ResolveKey(item);

            file.Strings.Add(new W3StringEntry { Id = id, Value = item.Text });

            // Only an item that came out carrying a key gets one: an item without a key is reached by its
            // id rather than by any key.
            if (item.KeyHex.Length == 0) continue;
            keys.Add((uint.Parse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture), id,
                item.KeyName));
        }

        return mayWrite;
    }

    /// <summary>
    ///     Reports every id that several entries share
    /// </summary>
    /// <param name="file">The container whose entries are reported on</param>
    /// <param name="mayWrite">Set to false, because an id has to be unique</param>
    private static void ReportDuplicateIds(W3StringsFile file, ref bool mayWrite)
    {
        foreach (var group in file.Strings.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
        {
            Log.Error(
                "String ID {StringId} is used by {Count} entries, so the container cannot be looked up",
                group.Key, group.Count());
            mayWrite = false;
        }
    }

    /// <summary>
    ///     Reports every hash that several entries share
    /// </summary>
    /// <param name="keys">The keys the entries carry, as they arrived</param>
    /// <param name="mayWrite">Set to false, because a key has to be unique</param>
    private static void ReportDuplicateKeys(List<(uint Hash, uint Id, string KeyName)> keys, ref bool mayWrite)
    {
        // One hash claimed by several ids: only one of them can ever be looked up by that key.
        foreach (var group in keys.GroupBy(key => key.Hash).Where(group => group.Count() > 1))
        {
            var ids = group.Select(key => key.Id).Distinct().Order().ToArray();
            var name = group.First().KeyName is { Length: > 0 } named ? $" (named {named})" : string.Empty;
            Log.Error(
                "Key hash 0x{Hash:X8}{Name} points at {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                group.Key, name, ids.Length, string.Join(", ", ids));
            mayWrite = false;
        }

        // A name is only input that gets hashed, so two entries carrying one name are two entries claiming
        // one hash, whether they write the name the same way or only in a different case.
        foreach (var group in keys.Where(key => key.KeyName is { Length: > 0 })
                     .GroupBy(key => key.KeyName, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            var ids = group.Select(key => key.Id).Distinct().Order().ToArray();
            Log.Error(
                "The key name {KeyName} is used by {Count} different string IDs ({StringIds}), so only one of them can be looked up",
                group.Key, ids.Length, string.Join(", ", ids));
            mayWrite = false;
        }
    }

    /// <summary>
    ///     Lays the key block of the container down from the keys its entries carry
    /// </summary>
    /// <param name="file">The container the keys are added to</param>
    /// <param name="keys">The keys the entries carry, as they arrived</param>
    /// <remarks>
    ///     One key entry is written for every entry that carries a key. Nothing is paired up or filtered out
    ///     beforehand: whether those entries add up as a container is decided by the rules that ran above
    /// </remarks>
    private static void AddKeys(W3StringsFile file, List<(uint Hash, uint Id, string KeyName)> keys)
    {
        foreach (var (hash, id, _) in keys)
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
    ///     Resolves the localization key of one item, and leaves the item carrying the key it is written with
    ///     The readable key name wins over the raw hash when both are set
    /// </summary>
    /// <param name="item">
    ///     The item to resolve the key of. It is left carrying the key it is written with, as the eight
    ///     digits of its hash, or carrying no text at all when it has no key
    /// </param>
    /// <remarks>
    ///     A name that is set is what the key is, and it is how a key is ordinarily authored: a container
    ///     stores hashes, so the hash of the name is the key the item is written with, and the hash the item
    ///     arrived with is only compared with it. An item with no name is written with the hash it carries,
    ///     when that text is one
    ///     A hash of zero is how either of them says it carries no key: an empty name is no name at all, and
    ///     it hashes to that very zero. Text that is not a 32-bit hexadecimal number is not a key either,
    ///     and it is reported rather than obeyed, because an item without a name has nothing left to be
    ///     looked up by then
    ///     Items arrive by reference, which is how the row a caller just saved comes to show the key it
    ///     was written with rather than the one it happened to carry
    /// </remarks>
    private static void ResolveKey(IStringItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            ResolveKeyFromName(item); // A name that is set is the key, whatever else the item carries
        else
            ResolveKeyFromHash(item); // Without a name, the key is the hash the item carries itself
    }

    /// <summary>
    ///     Resolves the key of an item that carries a readable name, which is the key it is written with
    /// </summary>
    /// <param name="item">The item whose name is the key, left carrying the hash the name stands for</param>
    /// <remarks>
    ///     The hash the item arrived with is only compared with the one its name stands for: the name is the
    ///     key either way, and a hash that is another key is reported rather than obeyed
    /// </remarks>
    private static void ResolveKeyFromName(IStringItem item)
    {
        var named = LocalizationKeyHash.Compute(item.KeyName); // Hash the readable localization key

        // An item that carries no hash at all has nothing to compare.
        if (!string.IsNullOrWhiteSpace(item.KeyHex))
        {
            // A hash of zero says the item carries no key, which is what no hash says as well, so only a
            // hash that is another key is worth a word, and text that is not a hash at all is one of those:
            // it is not this name's hash either.
            uint? carried = uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                out var hash)
                ? hash
                : null;
            if (carried != 0 && carried != named)
                Log.Warning(
                    "String ID {StringId} carries the key hash {KeyHash}, but its key name {KeyName} hashes to 0x{Named:X8}, so the name is the key it is written with",
                    item.StrId, item.KeyHex, item.KeyName, named);
        }

        // An empty name is no name at all, and it hashes to the very zero that says there is no key.
        item.KeyHex = named == 0 ? string.Empty : named.ToString("X8", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Resolves the key of an item that carries no name, which is the hash it carries itself
    /// </summary>
    /// <param name="item">The item whose hash is the key, left carrying it as the eight digits of a hash</param>
    /// <remarks>
    ///     Text that is not a 32-bit hexadecimal number is not a key, and a hash of zero is how a hash says
    ///     it carries no key: either way the item is left carrying no text at all, because there is nothing
    ///     left for anything to look it up by then
    /// </remarks>
    private static void ResolveKeyFromHash(IStringItem item)
    {
        if (string.IsNullOrWhiteSpace(item.KeyHex))
        {
            item.KeyHex = string.Empty;
            return;
        }

        if (uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var carried))
        {
            item.KeyHex = carried == 0 // A hash of zero says it carries no key, and no key is no text
                ? string.Empty
                : carried.ToString("X8", CultureInfo.InvariantCulture);
            return;
        }

        Log.Warning(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so it is not the key the item is written with",
            item.StrId, item.KeyHex);
        item.KeyHex = string.Empty;
    }
}