using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Builds a <see cref="W3StringsFile" /> from a list of string items.
/// </summary>
internal static class W3StringsBuilder
{
    /// <summary>
    ///     Builds a container from the given items, or null when they cannot be written.
    /// </summary>
    /// <param name="items">The items to build the container from.</param>
    /// <param name="key">The language key of the container.</param>
    /// <returns>The built container, or null when the items are invalid.</returns>
    /// <remarks>
    ///     The version is not asked for here: it is chosen when the container is written.
    /// </remarks>
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint key)
    {
        if (HasDuplicateStrIds(items)) return null;
        var file = new W3StringsFile { Key = key };
        return items.Any(item => !ProcessItem(item, file)) ? null : file;
    }

    /// <summary>
    ///     Parses the id of an item and adds its entries to the container.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="file">The container to add the item to.</param>
    /// <returns>True if the item is valid.</returns>
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
    ///     Resolves the key of an item and adds its entries to the container.
    /// </summary>
    /// <param name="file">The container to add the entries to.</param>
    /// <param name="id">The id of the entries.</param>
    /// <param name="item">The item to add the entries for.</param>
    private static void ResolveKeyAndAddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        ResolveKey(item);
        AddEntry(file, id, item);
    }

    /// <summary>
    ///     Adds the string entry of an item, and its key entry when it carries a key.
    /// </summary>
    /// <param name="file">The container to add the entries to.</param>
    /// <param name="id">The id of the entries.</param>
    /// <param name="item">The item to add the entries for.</param>
    private static void AddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        // An item with no key gets no key entry: writing one with a hash of zero would leave an entry no
        // key can resolve to.
        if (GetCarriedKeyHash(item) is > 0) AddKeyEntry(file, id, item);
        AddStringEntry(file, id, item);
    }

    /// <summary>
    ///     Adds the string entry of an item to the container.
    /// </summary>
    /// <param name="file">The container to add the entry to.</param>
    /// <param name="id">The id of the entry.</param>
    /// <param name="item">The item to add the entry for.</param>
    private static void AddStringEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Strings.Add(CreateStringEntry(id, item));
    }

    /// <summary>
    ///     Adds the key entry of an item to the container.
    /// </summary>
    /// <param name="file">The container to add the entry to.</param>
    /// <param name="id">The id of the entry.</param>
    /// <param name="item">The item to add the entry for.</param>
    private static void AddKeyEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Keys.Add(CreateKeyEntry(id, item));
    }

    /// <summary>
    ///     Creates a string entry from an item.
    /// </summary>
    /// <param name="id">The id of the entry.</param>
    /// <param name="item">The item to create the entry from.</param>
    /// <returns>The created entry.</returns>
    private static W3StringEntry CreateStringEntry(uint id, IStringItem item)
    {
        return new W3StringEntry
        {
            Id = id,
            Value = item.Text
        };
    }

    /// <summary>
    ///     Creates a key entry from an item.
    /// </summary>
    /// <param name="id">The id of the entry.</param>
    /// <param name="item">The item to create the entry from.</param>
    /// <returns>The created entry.</returns>
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
    ///     Checks whether the items hold duplicate string ids.
    /// </summary>
    /// <param name="items">The items to check.</param>
    /// <returns>True when two items share a string id.</returns>
    private static bool HasDuplicateStrIds(IReadOnlyList<IStringItem> items)
    {
        if (items.DistinctBy(x => x.StrId).Count() >= items.Count) return false;
        Log.Error("The container has multiple items with the same string id");
        return true;
    }

    /// <summary>
    ///     Resolves the key of an item, from its name when it has one and otherwise from its hash.
    /// </summary>
    /// <param name="item">The item to resolve the key for.</param>
    private static void ResolveKey(IStringItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            ResolveKeyFromName(item);
        else
            ResolveKeyFromHash(item);
    }

    /// <summary>
    ///     Resolves the key of an item from its key name.
    /// </summary>
    /// <param name="item">The item to resolve the key for.</param>
    private static void ResolveKeyFromName(IStringItem item)
    {
        var named = LocalizationKeyHash.Of(item.KeyName);
        WarnIfPreExistingKeyHashInconsistent(item, named.Hash);
        SetKeyHexFromHash(item, named.Hash);
    }

    /// <summary>
    ///     Warns when an item carries both a key name and an inconsistent key hash.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <param name="named">The hash of the key name.</param>
    private static void WarnIfPreExistingKeyHashInconsistent(IStringItem item, uint named)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyHex)) WarnIfKeyHashInconsistent(item, named);
    }

    /// <summary>
    ///     Warns that the carried key hash does not match the hash of the key name.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <param name="named">The hash of the key name.</param>
    private static void WarnIfKeyHashInconsistent(IStringItem item, uint named)
    {
        LogInconsistentKeyHash(item, named, GetCarriedKeyHash(item));
    }

    /// <summary>
    ///     Gets the key hash an item carries, when it is a valid 32-bit hexadecimal number.
    /// </summary>
    /// <param name="item">The item to read the key hash from.</param>
    /// <returns>The key hash, or null when none is carried.</returns>
    private static uint? GetCarriedKeyHash(IStringItem item)
    {
        return uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash)
            ? hash
            : null;
    }

    /// <summary>
    ///     Logs a warning when an item carries a key hash that disagrees with its key name.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <param name="named">The hash of the key name.</param>
    /// <param name="carried">The key hash the item carries, or null when none is carried.</param>
    private static void LogInconsistentKeyHash(IStringItem item, uint named, uint? carried)
    {
        if (carried != 0 && carried != named)
            Log.Warning(
                "String ID {StringId} carries the key hash {KeyHash}, but its key name {KeyName} hashes to 0x{Named:X8}, so the name is the key it is written with",
                item.StrId, item.KeyHex, item.KeyName, named);
    }

    /// <summary>
    ///     Resolves the key of an item from the hash it carries.
    /// </summary>
    /// <param name="item">The item to resolve the key for.</param>
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
    ///     Sets the key hash of an item to the given hash.
    /// </summary>
    /// <param name="item">The item to set the key hash for.</param>
    /// <param name="hash">The hash to set the key hash to.</param>
    private static void SetKeyHexFromHash(IStringItem item, uint hash)
    {
        item.KeyHex = hash != 0
            ? hash.ToString("X8", CultureInfo.InvariantCulture)
            : string.Empty;
    }
}