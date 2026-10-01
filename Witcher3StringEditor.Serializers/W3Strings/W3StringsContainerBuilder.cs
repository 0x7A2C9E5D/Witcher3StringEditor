using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

internal static class W3StringsContainerBuilder
{
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key)
    {
        if (HasDuplicateStrIds(items)) return null;

        var file = new W3StringsFile { Version = version, Key = key };

        foreach (var item in items)
            if (uint.TryParse(item.StrId, out var id))
            {
                ResolveKeyAndAddEntry(file, id, item);
            }
            else
            {
                Log.Error("String ID {StringId} is not a 32-bit unsigned integer", item.StrId);
                return null;
            }

        return file;
    }

    private static void ResolveKeyAndAddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        ResolveKey(item);
        AddEntry(file, id, item);
    }

    private static void AddEntry(W3StringsFile file, uint id, IStringItem item)
    {
        file.Keys.Add(CreateKeyEntry(id, item));
        file.Strings.Add(CreateStringEntry(id, item));
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

        if (!string.IsNullOrWhiteSpace(item.KeyHex))
        {
            uint? carried = uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                out var hash)
                ? hash
                : null;
            if (carried != 0 && carried != named)
                Log.Warning(
                    "String ID {StringId} carries the key hash {KeyHash}, but its key name {KeyName} hashes to 0x{Named:X8}, so the name is the key it is written with",
                    item.StrId, item.KeyHex, item.KeyName, named);
        }

        item.KeyHex = named == 0 ? string.Empty : named.ToString("X8", CultureInfo.InvariantCulture);
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
            item.KeyHex = carried == 0
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