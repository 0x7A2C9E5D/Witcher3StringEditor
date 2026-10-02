using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Builds a W3StringsFile from a list of IStringItems
/// </summary>
internal static class W3StringsBuilder
{
    /// <summary>
    ///     Builds a W3StringsFile from a list of IStringItems
    /// </summary>
    /// <param name="items">The list of IStringItems to build the W3StringsFile from</param>
    /// <param name="version">The version of the W3StringsFile</param>
    /// <param name="key">The language key of the W3StringsFile</param>
    /// <returns>The built W3StringsFile</returns>
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key)
    {
        if (HasDuplicateStrIds(items)) return null;
        var file = new W3StringsFile { Version = version, Key = key };
        return items.Any(item => !ProcessItem(item, file)) ? null : file;
    }

    /// <summary>
    ///     Processes an IStringItem and adds it to the W3StringsFile
    /// </summary>
    /// <param name="item">The IStringItem to process</param>
    /// <param name="file">The W3StringsFile to add the item to</param>
    /// <returns>True if the item is valid, false otherwise</returns>
    private static bool ProcessItem(IStringItem item, W3StringsFile file)
    {
        if (uint.TryParse(item.StrId, out var id))
        {
            ResolveKeyAndAddEntry(file, id, item);
            return true;
        }

        Log.Error("String ID {StringId} is not a 32-bit unsigned integer", item.StrId);
        return false;
    }

    /// <summary>
    ///     Resolves the key of an IStringItem and adds the entry to the W3StringsFile
    /// </summary>
    /// <param name="file">The W3StringsFile to add the entry to</param>
    /// <param name="id">The id of the entry</param>
    /// <param name="item">The IStringItem to process</param>
    /// <summary>
    ///     Resolves the key for the given IStringItem and adds it to the W3StringsFile
    /// </summary>
    /// <param name="file">The W3StringsFile to add the key to</param>
    /// <param name="id">The ID of the key</param>
    /// <param name="item">The IStringItem to resolve the key for</param>
    private static void ResolveKeyAndAddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        ResolveKey(item);
        AddEntry(file, id, item);
    }
    
    /// <summary>
    ///     Adds the key entry for the given IStringItem to the W3StringsFile
    /// </summary>
    /// <param name="file">The W3StringsFile to add the key entry to</param>
    /// <param name="id">The ID of the key entry</param>
    /// <param name="item">The IStringItem to add the key entry for</param>
    private static void AddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        AddKeyEntry(file, id, item);
        AddStringEntry(file, id, item);
    }

    /// <summary>
    ///     Adds the string entry for the given IStringItem to the W3StringsFile
    /// </summary>
    /// <param name="file">The W3StringsFile to add the string entry to</param>
    /// <param name="id">The ID of the string entry</param>
    /// <param name="item">The IStringItem to add the string entry for</param>
    private static void AddStringEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Strings.Add(CreateStringEntry(id, item));
    }

    /// <summary>
    ///     Adds the key entry for the given IStringItem to the W3StringsFile
    /// </summary>
    /// <param name="file">The W3StringsFile to add the key entry to</param>
    /// <param name="id">The ID of the key entry</param>
    /// <param name="item">The IStringItem to add the key entry for</param>
    private static void AddKeyEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Keys.Add(CreateKeyEntry(id, item));
    }

    /// <summary>
    ///     Creates a W3StringEntry from an IStringItem
    /// </summary>
    /// <param name="id">The ID of the string entry</param>
    /// <param name="item">The IStringItem to create the string entry from</param>
    /// <returns>The created W3StringEntry</returns>
    private static W3StringEntry CreateStringEntry(uint id, IStringItem item)
    {
        return new W3StringEntry
        {
            Id = id,
            Value = item.Text
        };
    }

    /// <summary>
    ///     Creates a W3KeyEntry from an IStringItem
    /// </summary>
    /// <param name="id">The ID of the key entry</param>
    /// <param name="item">The IStringItem to create the key entry from</param>
    /// <returns>The created W3KeyEntry</returns>
    private static W3KeyEntry CreateKeyEntry(uint id, IStringItem item)
    {
        return new W3KeyEntry
        {
            Id = id,
            // The key is carried as hexadecimal text, and a cleared key means the hash is zero
            KeyHash = string.IsNullOrWhiteSpace(item.KeyHex)
                ? 0
                : uint.Parse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    ///     Checks if the given list of IStringItems has duplicate string ids
    /// </summary>
    /// <param name="items">The list of IStringItems to check</param>
    /// <returns>True if the list has duplicate string ids, false otherwise</returns>
    private static bool HasDuplicateStrIds(IReadOnlyList<IStringItem> items)
    {
        if (items.DistinctBy(x => x.StrId).Count() >= items.Count) return false;
        Log.Error("The container has multiple items with the same string id");
        return true;
    }

    /// <summary>
    ///     Resolves the key of an IStringItem
    /// </summary>
    /// <param name="item">The IStringItem to resolve the key for</param>
    private static void ResolveKey(IStringItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            ResolveKeyFromName(item);
        else
            ResolveKeyFromHash(item);
    }

    /// <summary>
    ///     Resolves the key of an IStringItem from the key name
    /// </summary>
    /// <param name="item">The IStringItem to resolve the key for</param>
    private static void ResolveKeyFromName(IStringItem item)
    {
        var named = LocalizationKeyHash.Compute(item.KeyName);
        WarnIfPreExistingKeyHashInconsistent(item, named);
        SetKeyHexFromHash(item, named);
    }

    /// <summary>
    ///     Warns if the key hash of the given IStringItem is inconsistent with the key name
    /// </summary>
    /// <param name="item">The IStringItem to check</param>
    /// <param name="named">The hash of the key name</param>
    private static void WarnIfPreExistingKeyHashInconsistent(IStringItem item, uint named)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyHex)) WarnIfKeyHashInconsistent(item, named);
    }

    /// <summary>
    ///     Warns if the key hash of the given IStringItem is inconsistent with the key name
    /// </summary>
    /// <param name="item">The IStringItem to check</param>
    /// <param name="named">The hash of the key name</param>
    private static void WarnIfKeyHashInconsistent(IStringItem item, uint named)
    {
        LogInconsistentKeyHash(item, named, GetCarriedKeyHash(item));
    }

    /// <summary>
    ///     Gets the key hash carried in the given IStringItem
    /// </summary>
    /// <param name="item">The IStringItem to get the key hash from</param>
    /// <returns>The key hash carried in the IStringItem, or null if none is carried</returns>
    /// <summary>
    ///     Gets the key hash of the given IStringItem, if it is a valid 32-bit hexadecimal number
    /// </summary>
    /// <param name="item">The IStringItem to get the key hash for</param>
    /// <returns>The key hash of the IStringItem, or null if it is not a valid 32-bit hexadecimal number</returns>
    private static uint? GetCarriedKeyHash(IStringItem item)
    {
        return uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash)
            ? hash
            : null;
    }

    /// <summary>
    ///     Logs a warning if the key hash of the given IStringItem is inconsistent with the key name
    /// </summary>
    /// <param name="item">The IStringItem to check</param>
    /// <param name="named">The hash of the key name</param>
    /// <param name="carried">The key hash carried in the IStringItem, or null if none is carried</param>
    private static void LogInconsistentKeyHash(IStringItem item, uint named, uint? carried)
    {
        if (carried != 0 && carried != named)
            Log.Warning(
                "String ID {StringId} carries the key hash {KeyHash}, but its key name {KeyName} hashes to 0x{Named:X8}, so the name is the key it is written with",
                item.StrId, item.KeyHex, item.KeyName, named);
    }

    /// <summary>
    ///     Resolves the key of an IStringItem from the key hash
    /// </summary>
    /// <param name="item">The IStringItem to resolve the key for</param>
    private static void ResolveKeyFromHash(IStringItem item)
    {
        if (string.IsNullOrWhiteSpace(item.KeyHex))
        {
            item.KeyHex = string.Empty;
            return;
        }

        if (uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var carried))
        {
            SetKeyHexFromHash(item, carried);
            return;
        }

        Log.Warning(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so it is not the key the item is written with",
            item.StrId, item.KeyHex);
        item.KeyHex = string.Empty;
    }

    /// <summary>
    ///     Sets the key hash of the given IStringItem to the given hash
    /// </summary>
    /// <param name="item">The IStringItem to set the key hash for</param>
    /// <param name="hash">The hash to set the key hash to</param>
    private static void SetKeyHexFromHash(IStringItem item, uint hash)
    {
        item.KeyHex = hash != 0
            ? hash.ToString("X8", CultureInfo.InvariantCulture)
            : string.Empty;
    }
}