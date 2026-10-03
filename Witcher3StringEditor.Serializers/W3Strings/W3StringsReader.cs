using System.Text;
using Serilog;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Reads a w3strings container out of a stream
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
            var buffer = ReadBuffer(reader, key2Offset, head.Version);
            var keptEntries = KeepEntriesInBuffer(entries, buffer); // The entries whose text the file holds

            var storedTexts = ReadStoredTexts(input, keptEntries, buffer);

            // The key closes the container with its tail half, and the magic every id and stored text was
            // obfuscated with follows from it.
            var key = head.Key | ReadKey2(reader, key2Offset);

            // The buffer answers with the version the container is stored in, which is the one it declared
            // unless it declares one generation and holds the other. The container itself holds the texts
            // and their keys only: the version it is written with is chosen when it is written.
            var container = Assemble(key, keptEntries, keys, storedTexts,
                W3StringsFormat.OffsetUnitSize(buffer.Version));
            Log.Information("Read a W3Strings v{Version} container (magic {Magic})", buffer.Version,
                container.Magic == 0 ? "none" : $"0x{container.Magic:X8}");
            return container;
        }
        catch (Exception ex) when (ex is not W3StringsException)
        {
            // The codec answers with one exception type, with whatever went wrong below as its cause.
            throw new W3StringsException("The stream does not hold a readable w3strings container", ex);
        }
    }

    /// <summary>
    ///     Reads the head of a container: its magic, the version and the head half of the language key
    /// </summary>
    /// <param name="reader">The reader the head comes from</param>
    /// <param name="length">The length of the container</param>
    /// <returns>
    ///     The version the container was written with, and the language key with its head half in place and
    ///     still waiting for its tail half
    /// </returns>
    /// <exception cref="W3StringsException">Thrown when the stream is too short, or is not a container at all</exception>
    private static (uint Version, uint Key) ReadHead(BinaryReader reader, long length)
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
        return (version, key);
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
    ///     Reads the size the container gives its string buffer, which holds every text of it, and works out
    ///     the version the buffer is stored in
    /// </summary>
    /// <param name="reader">The reader the size comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <param name="version">The version the container declares</param>
    /// <returns>
    ///     The offset the buffer starts at, in bytes, the size of it the file really holds, in units, and the
    ///     version its texts are stored in
    /// </returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when the buffer reaches past the end of the file and the container are not
    ///     one to read up to what the file holds
    /// </exception>
    /// <remarks>
    ///     The size is what holds every entry inside the buffer, and it is dropped as soon as the texts have
    ///     been read: the writer works the size of the buffer out from the texts it lays out in it.
    ///     A container of the UTF-8 generation that declares a larger buffer than the file holds is read up
    ///     to the end of the file with a warning, because that generation has a single reading: what is not
    ///     there cannot be decoded, and what is their stays readable
    /// </remarks>
    private static (long Start, uint Units, uint Version) ReadBuffer(BinaryReader reader, long key2Offset,
        uint version)
    {
        var units = SectionCount.Read(reader.BaseStream);
        var start = reader.BaseStream.Position;
        var stored = StoredVersion(version, start, units, key2Offset);
        var unit = W3StringsFormat.OffsetUnitSize(stored);
        var heldUnits = Math.Max(0, key2Offset - start) / unit; // How many units of the buffer the file has
        if (units * unit <= key2Offset) return (start, units, stored);

        if (stored < W3StringsFormat.FirstUtf8Version)
            throw new W3StringsException(
                $"string buffer overruns file ({start + units * unit} > {key2Offset})");

        Log.Warning(
            "The container declares a string buffer of {Declared} unit(s), of which the file holds {HeldUnits}: " +
            "the buffer is read up to the end of the file",
            units, heldUnits);
        return (start, (uint)heldUnits, stored);
    }

    /// <summary>
    ///     Works out the version a container is really stored in
    /// </summary>
    /// <param name="version">The version the container declares</param>
    /// <param name="start">The offset the string buffer starts at</param>
    /// <param name="units">The size the container gives its string buffer, in units</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>The version the container is stored in, which is the declared one unless it holds the other generation</returns>
    /// <remarks>
    ///     The declared version answers how the buffer is counted for every container that declares it
    ///     honestly: a container of the UTF-8 generation counts it in bytes, one of the older generation in
    ///     characters of two bytes. Some community containers declare the older version while storing UTF-8,
    ///     which makes every offset, length and buffer size of them twice the number they are, so the
    ///     declaration is measured against the file instead of being taken for granted: the buffer of a
    ///     well-formed container ends exactly where the language key that closes the file begins, and a
    ///     container that only reaches that byte when its buffer is counted in bytes holds UTF-8 whatever it
    ///     declares. Both corrections are logged
    /// </remarks>
    private static uint StoredVersion(uint version, long start, uint units, long key2Offset)
    {
        var declared = W3StringsFormat.OffsetUnitSize(version);
        if (declared == 1) return version; // The UTF-8 generation is read as the version it declares
        if (units == 0) return OlderGeneration(version); // No buffer to measure, so the declaration stands

        var asDeclared = start + units * declared; // Where the buffer ends read as the version says
        var asUtf8 = start + units; // Where it ends read as UTF-8
        // The buffer ends where the language key begins, or, failing that, the reading the version asks
        // for does not fit the file at all and the other one does.
        var isStoredAsUtf8 = asUtf8 == key2Offset || (asDeclared > key2Offset && asUtf8 <= key2Offset);
        if (!isStoredAsUtf8) return OlderGeneration(version);

        Log.Warning(
            "The container declares version {Declared}, which counts its texts in characters of two bytes, " +
            "but its string buffer only ends where the language key begins when it is counted in bytes: its " +
            "texts are stored as UTF-8, so the container is read as version {Stored} instead of the version " +
            "it declares",
            version, W3StringsFormat.FirstUtf8Version);
        return W3StringsFormat.FirstUtf8Version;
    }

    /// <summary>
    ///     Answers the version a container of the older generation is read as, which is the one that generation
    ///     is described by
    /// </summary>
    /// <param name="version">The version the container declares</param>
    /// <returns>The version 162, which the older generation of the game has one layout for</returns>
    /// <remarks>
    ///     The older generation has a single layout, and 162 is the version that describes it: a container
    ///     that declares another version of it, 163 among them, is read as 162 and the correction is logged,
    ///     because only the layout matters to a reader and the difference between those versions is not one
    ///     this build could tell
    /// </remarks>
    private static uint OlderGeneration(uint version)
    {
        if (version == W3StringsFormat.Utf16LeVersion) return version;

        Log.Warning(
            "The container declares version {Declared}, which is read as version {Stored}: the older " +
            "generation of the game has one layout",
            version, W3StringsFormat.Utf16LeVersion);
        return W3StringsFormat.Utf16LeVersion;
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
    ///     Assembles the container out of the sections that were read, decoding every id and text on the way
    /// </summary>
    /// <param name="key">The full language key, which the magic every id and stored text was obfuscated with follows from</param>
    /// <param name="entries">The entries of the first block, whose ids are still obfuscated and whose texts not yet decoded</param>
    /// <param name="keys">The entries of the second block, whose ids are still obfuscated</param>
    /// <param name="storedTexts">The stored bytes of every entry, in entry order, which decoding consumes</param>
    /// <param name="unit">The number of bytes one character takes in the version the container is stored in</param>
    /// <returns>The container</returns>
    private static W3StringsFile Assemble(uint key, W3StringEntry[] entries, W3KeyEntry[] keys,
        byte[][] storedTexts, int unit)
    {
        var magic = W3StringsFormat.MagicOf(key); // The magic every id and stored text was obfuscated with
        var file = new W3StringsFile { Key = key };

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
    /// <remarks>
    ///     A block is not read up to what the file holds the way the string buffer is: every section after it
    ///     starts where the block ends, so an entry count that does not fit leaves anything to read the rest
    ///     of the container with, and the file is refused instead of being guessed at
    /// </remarks>
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
    ///     Keeps the entries whose text the string buffer holds
    /// </summary>
    /// <param name="entries">The entries of the first block, in entry order</param>
    /// <param name="buffer">The extent of the string buffer, and the version its texts are stored in</param>
    /// <returns>The entries whose text lies inside the buffer, in entry order</returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when an entry points outside the buffer of a container that is not read up
    ///     to what the file holds
    /// </exception>
    /// <remarks>
    ///     A container of the older generation has two readings, so a text that is not inside the buffer it
    ///     declared is not one to drop: the reading may be the wrong one, and the file is refused instead.
    ///     The UTF-8 generation has a single reading, so an entry the file does not hold is dropped with one
    ///     warning for all of them, which leaves every text that is there readable
    /// </remarks>
    private static W3StringEntry[] KeepEntriesInBuffer(W3StringEntry[] entries,
        (long Start, uint Units, uint Version) buffer)
    {
        var outside = Array.FindIndex(entries, entry => (long)entry.Offset + entry.Length > buffer.Units);
        if (outside < 0) return entries; // Every text is inside the buffer, which is what a container holds

        if (buffer.Version < W3StringsFormat.FirstUtf8Version)
            throw new W3StringsException(
                $"string entry #{outside} points outside the string buffer " +
                $"(offset {entries[outside].Offset}, length {entries[outside].Length}, buffer {buffer.Units} unit(s))");

        var keptEntries = entries.Where(entry => (long)entry.Offset + entry.Length <= buffer.Units).ToArray();
        Log.Warning(
            "{Dropped} of {Declared} string entries reach past the {Units} unit(s) of string buffer the file " +
            "holds: they are dropped",
            entries.Length - keptEntries.Length, entries.Length, buffer.Units);
        return keptEntries;
    }

    /// <summary>
    ///     Reads the stored bytes of every text out of the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="entries">The entries of the first block, in entry order, every one of them inside the buffer</param>
    /// <param name="buffer">The extent of the string buffer, and the version its texts are stored in</param>
    /// <returns>The stored bytes of every entry, in entry order</returns>
    private static byte[][] ReadStoredTexts(Stream input, W3StringEntry[] entries,
        (long Start, uint Units, uint Version) buffer)
    {
        var storedTexts = new byte[entries.Length][];
        var cursor = buffer.Start;
        var unit = W3StringsFormat.OffsetUnitSize(buffer.Version); // The bytes one character takes

        // Entries are fetched in the order they occupy the buffer, which is the order they are listed in
        // for every container this build writes, so the stream simply keeps moving forward. Every one of
        // them was checked against the buffer before this, so none of them reaches past it.
        foreach (var i in Enumerable.Range(0, entries.Length).OrderBy(index => entries[index].Offset))
        {
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