using System.Globalization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Serializers.W3Strings;

internal static class W3StringsContainerBuilder
{
    public static W3StringsFile? Build(IReadOnlyList<IStringItem> items, uint version, uint key)
    {
        if (items.DistinctBy(x => x.StrId).Count() < items.Count)
        {
            Log.Error("The container has multiple items with the same string id");
            return Refuse(items);
        }

        if (!ReadIds(items)) return Refuse(items);

        var file = new W3StringsFile { Version = version, Key = key };
        AddStrings(file, items);

        foreach (var item in items) ResolveKey(item);

        if (!ReportDuplicateKey(items)) return Refuse(items);

        AddKeys(file, items);
        if (file.Keys.Count != 0) return file;
        Log.Error(
            "None of the {Count} item(s) carries a key, and the game's encoder cannot read a container without a key block",
            items.Count);
        return Refuse(items);
    }


    private static W3StringsFile? Refuse(IReadOnlyList<IStringItem> items)
    {
        Log.Error("Refusing to write {Count} item(s): the container holds errors", items.Count);
        return null;
    }


    private static void AddStrings(W3StringsFile file, IReadOnlyList<IStringItem> items)
    {
        foreach (var item in items)
            file.Strings.Add(new W3StringEntry
            {
                Id = uint.Parse(item.StrId, NumberStyles.None, CultureInfo.InvariantCulture),
                Value = item.Text
            });
    }


    private static bool ReadIds(IReadOnlyList<IStringItem> items)
    {
        HashSet<uint> ids = [];
        foreach (var strId in items.Select(item => item.StrId))
        {
            if (!TryParseId(strId, out var id))
            {
                Log.Error(
                    "String ID {StringId} is not a 32-bit unsigned integer, so the container is refused rather than written without it",
                    strId);
                return false;
            }

            if (ids.Add(id)) continue;

            Log.Error("String ID {Id} is used by more than one entry, so the container cannot be looked up", id);
            return false;
        }

        return true;
    }


    private static bool ReportDuplicateKey(IReadOnlyList<IStringItem> items)
    {
        var duplicated = items.Where(item => item.KeyHex.Length != 0).GroupBy(item => item.KeyHex)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicated is null) return true;

        var ids = duplicated.Select(item => uint.Parse(item.StrId, NumberStyles.None, CultureInfo.InvariantCulture))
            .Distinct().Order().ToArray();
        var name = duplicated.First().KeyName is { Length: > 0 } named ? $" (named {named})" : string.Empty;
        Log.Error(
            "Key hash 0x{Hash:l}{Name:l} points at {Count} different string IDs ({StringIds:l}), so only one of them can be looked up",
            duplicated.Key, name, ids.Length, string.Join(", ", ids));
        return false;
    }

    private static void AddKeys(W3StringsFile file, IReadOnlyList<IStringItem> items)
    {
        foreach (var item in items)
        {
            if (item.KeyHex.Length == 0) continue; // An item that carries no key is looked up by its id

            file.Keys.Add(new W3KeyEntry
            {
                KeyHash = uint.Parse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                Id = uint.Parse(item.StrId, NumberStyles.None, CultureInfo.InvariantCulture)
            });
        }
    }

    private static bool TryParseId(string strId, out uint id)
    {
        return uint.TryParse(strId, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }

    private static void ResolveKey(IStringItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.KeyName))
            ResolveKeyFromName(item); // A name that is set is the key, whatever else the item carries
        else
            ResolveKeyFromHash(item); // Without a name, the key is the hash the item carries itself
    }


    private static void ResolveKeyFromName(IStringItem item)
    {
        var named = LocalizationKeyHash.Compute(item.KeyName);

        if (!string.IsNullOrWhiteSpace(item.KeyHex))
        {
            uint? carried =
                uint.TryParse(item.KeyHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash)
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
            item.KeyHex = carried == 0 ? string.Empty : carried.ToString("X8", CultureInfo.InvariantCulture);
            return;
        }

        Log.Warning(
            "String ID {StringId} carries the key hash {KeyHash}, which is not a 32-bit hexadecimal number, so it is not the key the item is written with",
            item.StrId, item.KeyHex);
        item.KeyHex = string.Empty;
    }
}