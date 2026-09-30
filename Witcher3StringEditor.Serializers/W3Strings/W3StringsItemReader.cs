using System.Globalization;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Turns the entries of a container into the items they are shown as
///     The reverse of what <see cref="W3StringsContainerBuilder" /> does, and the only place the ids and
///     key hashes of a container are written out as text: a caller that reads a file gets items back and
///     never has to know how either of them is stored
/// </summary>
internal static class W3StringsItemReader
{
    /// <summary>
    ///     Reads the entries of a container into items
    /// </summary>
    /// <param name="file">The container to read the items of</param>
    /// <returns>The items of the container, in container order</returns>
    public static List<W3StringItem> ReadItems(W3StringsFile file)
    {
        // Block 2 maps a localization-key hash to the id it resolves to. An id can carry several keys,
        // so the first hash found is the one shown next to the entry.
        var keyHashes = new Dictionary<uint, uint>(file.Keys.Count);
        foreach (var key in file.Keys)
            keyHashes.TryAdd(key.Id, key.KeyHash);

        var items = new List<W3StringItem>(file.Strings.Count);
        items.AddRange(file.Strings.Select(entry => new W3StringItem
        {
            StrId = entry.Id.ToString(CultureInfo.InvariantCulture), // The string id, as text
            KeyName = string.Empty, // A container keeps hashes, not the names they were computed from
            KeyHex = keyHashes.TryGetValue(entry.Id, out var keyHash)
                ? keyHash.ToString("X8", CultureInfo.InvariantCulture)
                : string.Empty, // The localization key hash, when the entry has one
            Text = entry.Value // The decoded text
        }));
        return items;
    }
}