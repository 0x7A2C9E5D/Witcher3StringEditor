using System.Globalization;
using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Reads a w3strings container out of a stream, and turns what it holds into the items they are shown as
///     The container is read section by section and never held whole: the first block says where every
///     text sits, so the stored bytes are fetched one at a time and only the model keeps them
/// </summary>
internal static class W3StringsReader
{
    /// <summary>
    ///     Reads a container from a file stream
    /// </summary>
    /// <param name="input">The stream to read from, which a caller opens over a file</param>
    /// <returns>The container</returns>
    /// <exception cref="W3StringsException">Thrown when the stream does not hold a container this build can decode</exception>
    /// <remarks>
    ///     The offsets of the first block point into a buffer that follows it, and the tail half of the
    ///     language key sits in the last two bytes of the container. Reaching both out of order is why
    ///     the stream is read by offset: over a file that is what a stream is there for
    /// </remarks>
    public static W3StringsFile Read(Stream input)
    {
        try
        {
            // The last two bytes hold the tail half of the language key, so no section may reach into them.
            var key2Offset = input.Length - 2;

            using var reader = new BinaryReader(input, Encoding.UTF8, true); // The stream stays open for the caller
            var head = ReadHead(reader, input.Length);

            // The layout of the stored bytes is needed before any text can be decoded.
            var entries = ReadStringEntries(reader, key2Offset);
            var keys = ReadKeys(reader, key2Offset);
            var buffer = ReadBuffer(reader, key2Offset, head.Unit);

            var storedTexts = ReadStoredTexts(input, entries, buffer, head.Unit);

            // The key closes the container with its tail half, and it is what the magic every id and
            // stored text was obfuscated with follows from.
            var key = head.Key | ReadKey2(reader, key2Offset);
            var magic = W3StringsFormat.MagicOf(key);

            return ToFile((head.Version, key, head.Unit), magic, entries, keys, storedTexts, head.Unit);
        }
        catch (Exception ex) when (ex is not W3StringsException)
        {
            // The codec answers with one exception type, with whatever went wrong below as its cause.
            throw new W3StringsException("The stream does not hold a readable w3strings container", ex);
        }
    }

    /// <summary>
    ///     Reads the entries of a container into the items they are shown as
    /// </summary>
    /// <param name="file">The container to read the items of</param>
    /// <returns>The items of the container, in container order</returns>
    /// <remarks>
    ///     The reverse of what <see cref="W3StringsBuilder" /> does, and the only place the ids and
    ///     key hashes of a container are written out as text: a caller that reads a file gets items back and
    ///     never has to know how either of them is stored
    /// </remarks>
    public static List<StringItem> ReadItems(W3StringsFile file)
    {
        // Block 2 maps a localization-key hash to the id it resolves to. An id can carry several keys,
        // so the first hash found is the one shown next to the entry.
        var keyHashes = new Dictionary<uint, uint>(file.Keys.Count);
        foreach (var key in file.Keys)
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

    /// <summary>
    ///     Reads the head of a container: its magic, the version and the head half of the language key
    /// </summary>
    /// <param name="reader">The reader the head comes from</param>
    /// <param name="length">The length of the container</param>
    /// <returns>
    ///     The version the container was written with, the language key with its head half in place and
    ///     still waiting for its tail half, and the number of bytes one character takes in it
    /// </returns>
    /// <exception cref="W3StringsException">Thrown when the stream is too short, or is not a container at all</exception>
    private static (uint Version, uint Key, int Unit) ReadHead(BinaryReader reader, long length)
    {
        if (length < W3StringsFormat.MinSize)
            throw new W3StringsException($"file too small to be a w3strings container ({length} byte(s))");

        // The magic is compared as it is stored, so the error can quote what was actually found.
        var magic = reader.ReadBytes(W3StringsFormat.MagicBytes.Length);
        if (!magic.AsSpan().SequenceEqual(W3StringsFormat.MagicBytes))
            throw new W3StringsException(
                $"bad magic: expected \"RTSW\", got \"{Encoding.ASCII.GetString(magic)}\"");

        var version = reader.ReadUInt32();
        var key = (uint)reader.ReadUInt16() << 16; // The head half: the tail half closes the container
        return (version, key, W3StringsFormat.OffsetUnitSize(version));
    }

    /// <summary>
    ///     Reads the first block: where every text sits in the string buffer
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per text, without its stored bytes: the id is still obfuscated and the text not yet decoded</returns>
    private static W3StringEntry[] ReadStringEntries(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream),
            W3StringsFormat.Block1EntrySize, key2Offset, "block1");

        var entries = new W3StringEntry[count];
        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var length = reader.ReadUInt32();
            entries[i] = new W3StringEntry { Id = id, Offset = offset, Length = length };
        }

        return entries;
    }

    /// <summary>
    ///     Reads the second block: which key resolves to which id
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per key, whose id is still obfuscated</returns>
    private static W3KeyEntry[] ReadKeys(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream),
            W3StringsFormat.Block2EntrySize, key2Offset, "block2");

        var keys = new W3KeyEntry[count];
        for (var i = 0; i < count; i++)
        {
            var hash = reader.ReadUInt32();
            var id = reader.ReadUInt32();
            keys[i] = new W3KeyEntry { KeyHash = hash, Id = id }; // The stored id is decoded with the rest
        }

        return keys;
    }

    /// <summary>
    ///     Reads the size the container gives its string buffer, which holds every text of it
    /// </summary>
    /// <param name="reader">The reader the size comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The offset the buffer starts at, in bytes, and the size the container gives it, in units</returns>
    /// <exception cref="W3StringsException">Thrown when the buffer reaches past the end of the file</exception>
    /// <remarks>
    ///     The size is what holds every entry inside the buffer, and it is dropped as soon as the texts have
    ///     been read: the writer works the size of the buffer out from the texts it lays out in it
    /// </remarks>
    private static (long Start, uint Units) ReadBuffer(BinaryReader reader, long key2Offset, int unit)
    {
        var units = SectionCount.Read(reader.BaseStream);
        var start = reader.BaseStream.Position;
        var end = start + units * unit;
        return end > key2Offset
            ? throw new W3StringsException($"string buffer overruns file ({end} > {key2Offset})")
            : (start, units);
    }

    /// <summary>
    ///     Reads the tail half of the language key, which closes the container
    /// </summary>
    /// <param name="reader">The reader the key comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>The tail half of the language key</returns>
    private static ushort ReadKey2(BinaryReader reader, long key2Offset)
    {
        reader.BaseStream.Seek(key2Offset, SeekOrigin.Begin);
        return reader.ReadUInt16();
    }

    /// <summary>
    ///     Builds the container out of the sections that were read, decoding every text on the way
    /// </summary>
    /// <param name="head">The head of the container, holding the version and the full language key</param>
    /// <param name="magic">The magic every id and stored text was obfuscated with</param>
    /// <param name="entries">The entries of the first block, whose ids are still obfuscated and whose texts not yet decoded</param>
    /// <param name="keys">The entries of the second block, whose ids are still obfuscated</param>
    /// <param name="storedTexts">The stored bytes of every entry, in entry order, which decoding consumes</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The container</returns>
    private static W3StringsFile ToFile((uint Version, uint Key, int Unit) head, uint magic, W3StringEntry[] entries,
        W3KeyEntry[] keys, byte[][] storedTexts, int unit)
    {
        var file = new W3StringsFile
        {
            Version = head.Version,
            Key = head.Key
        };

        // The id is decoded and the text fetched, which is what turns every entry of the block into the
        // entry the container holds. Where a text sits and how long it was said to be stayed on the entry:
        // they are what the block stores, and a writer records them the same way as it lays the buffer out.
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            entry.Id ^= magic;
            entry.Value = StoredText.Decode(storedTexts[i], (int)entry.Length, magic, unit);
            file.Strings.Add(entry);
        }

        // The ids of the keys are decoded the way those of the strings are, which is what makes every
        // entry of the container hold what it says it holds.
        foreach (var entry in keys)
            file.Keys.Add(new W3KeyEntry { KeyHash = entry.KeyHash, Id = entry.Id ^ magic });

        return file;
    }

    /// <summary>
    ///     Checks that a block of fixed-size entries fits the file, and answers how many there are
    /// </summary>
    /// <param name="start">The offset the block starts at</param>
    /// <param name="count">The number of entries the container declares</param>
    /// <param name="entrySize">The size of one entry in bytes</param>
    /// <param name="key2Offset">The offset the language key at the end of the file starts at</param>
    /// <param name="name">The name of the block, used in the error message</param>
    /// <returns>The number of entries, as an index</returns>
    /// <exception cref="W3StringsException">Thrown when the block does not fit before the end of the file</exception>
    private static int CheckBlockFits(long start, uint count, int entrySize, long key2Offset, string name)
    {
        var end = start + count * entrySize;
        // The next section and, at the very least, the string buffer have to follow.
        if (end >= key2Offset)
            throw new W3StringsException(
                $"{name} declares {count} entries, which does not fit before the end of the file ({end} >= {key2Offset})");

        // A block that fits a file of this size cannot hold more entries than an index can address.
        return (int)count;
    }

    /// <summary>
    ///     Reads the stored bytes of every text out of the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="entries">The entries of the first block, in entry order</param>
    /// <param name="buffer">The extent of the string buffer</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The stored bytes of every entry, in entry order</returns>
    /// <exception cref="W3StringsException">Thrown when an entry points outside the string buffer</exception>
    private static byte[][] ReadStoredTexts(Stream input, W3StringEntry[] entries, (long Start, uint Units) buffer,
        int unit)
    {
        var storedTexts = new byte[entries.Length][];
        var cursor = buffer.Start;

        // Entries are fetched in the order they occupy the buffer, which is the order they are listed in
        // for every container this build writes, so the stream simply keeps moving forward.
        foreach (var i in Enumerable.Range(0, entries.Length).OrderBy(index => entries[index].Offset))
        {
            if ((long)entries[i].Offset + entries[i].Length > buffer.Units)
                throw new W3StringsException(
                    $"string entry #{i} points outside the string buffer " +
                    $"(offset {entries[i].Offset}, length {entries[i].Length}, buffer {buffer.Units} unit(s))");

            var at = buffer.Start + entries[i].Offset * unit;
            if (at < cursor) input.Seek(at, SeekOrigin.Begin); // An entry out of order has to be sought
            else Skip(input, at - cursor); // A gap is read past, which keeps the read sequential

            var stored = new byte[entries[i].Length * unit];
            input.ReadExactly(stored);
            storedTexts[i] = stored;
            cursor = at + stored.Length;
        }

        return storedTexts;
    }

    /// <summary>
    ///     Reads past a gap in the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="count">The number of bytes to read past</param>
    private static void Skip(Stream input, long count)
    {
        if (count <= 0) return;

        var scratch = new byte[(int)Math.Min(count, 4096)];
        while (count > 0)
        {
            var chunk = (int)Math.Min(count, scratch.Length);
            input.ReadExactly(scratch, 0, chunk);
            count -= chunk;
        }
    }
}