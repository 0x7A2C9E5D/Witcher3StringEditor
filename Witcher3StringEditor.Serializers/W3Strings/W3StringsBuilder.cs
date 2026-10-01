using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

internal static class W3StringsBuilder
{
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key)
    {
        if (HasDuplicateStrIds(items)) return null;
        var file = new W3StringsFile { Version = version, Key = key };
        return items.Any(item => !ProcessItem(item, file)) ? null : file;
    }

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

    private static void ResolveKeyAndAddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        ResolveKey(item);
        AddEntry(file, id, item);
    }

    private static void AddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        AddKeyEntry(file, id, item);
        AddStringEntry(file, id, item);
    }

    private static void AddStringEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Strings.Add(CreateStringEntry(id, item));
    }

    private static void AddKeyEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Keys.Add(CreateKeyEntry(id, item));
    }

    private static W3StringEntry CreateStringEntry(uint id, IStringItem item)
    {
        return new W3StringEntry
        {
            Id = id,
            Value = item.Text
        };
    }

    private static W3KeyEntry CreateKeyEntry(uint id, IStringItem item)
    {
        return new W3KeyEntry
        {
            Id = id,
            KeyHash = uint.Parse(item.KeyHex)
        };
    }

    private static bool HasDuplicateStrIds(IReadOnlyList<IStringItem> items)
    {
        if (items.DistinctBy(x => x.StrId).Count() >= items.Count) return false;
        Log.Error("The container has multiple items with the same string id");
        return true;
    }

    private static void ResolveKey(IStringItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            ResolveKeyFromName(item);
        else
            ResolveKeyFromHash(item);
    }


    private static void ResolveKeyFromName(IStringItem item)
    {
        var named = LocalizationKeyHash.Compute(item.KeyName);
        WarnIfPreExistingKeyHashInconsistent(item, named);
        SetKeyHexFromHash(item, named);
    }

    private static void WarnIfPreExistingKeyHashInconsistent(IStringItem item, uint named)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyHex)) WarnIfKeyHashInconsistent(item, named);
    }

    private static void WarnIfKeyHashInconsistent(IStringItem item, uint named)
    {
        LogInconsistentKeyHash(item, named, GetCarriedKeyHash(item));
    }

    private static uint? GetCarriedKeyHash(IStringItem item)
    {
        return uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash)
            ? hash
            : null;
    }

    private static void LogInconsistentKeyHash(IStringItem item, uint named, uint? carried)
    {
        if (carried != 0 && carried != named)
            Log.Warning(
                "String ID {StringId} carries the key hash {KeyHash}, but its key name {KeyName} hashes to 0x{Named:X8}, so the name is the key it is written with",
                item.StrId, item.KeyHex, item.KeyName, named);
    }

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

    private static void SetKeyHexFromHash(IStringItem item, uint hash)
    {
        item.KeyHex = hash != 0
            ? hash.ToString("X8", CultureInfo.InvariantCulture)
            : string.Empty;
    }
}
