using System.Globalization;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Reads the items a container is shown as out of it, the reverse of <see cref="W3StringsBuilder" />.
/// </summary>
internal static class W3StringsItemReader
{
    /// <summary>
    ///     Reads the entries of a container into the items they are shown as.
    /// </summary>
    /// <param name="file">The container to read the items of.</param>
    /// <returns>The items of the container, in container order.</returns>
    public static List<StringItem> Read(W3StringsFile file)
    {
        // Block 2 maps a localization-key hash to an id. A hash of zero means "no key" and is passed over,
        // and an id can carry several keys, so the first hash found is the one shown next to the entry.
        var keyHashes = new Dictionary<uint, uint>(file.Keys.Count);
        foreach (var key in file.Keys.Where(key => key.KeyHash != 0))
            keyHashes.TryAdd(key.Id, key.KeyHash);

        var items = new List<StringItem>(file.Strings.Count);
        items.AddRange(file.Strings.Select(entry => new StringItem
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