using System.Text;
using Serilog;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The storage model of the w3strings format: the fixed facts of the format, and the stored form of one
///     container.
/// </summary>
/// <remarks>
///     The format itself (magic, section sizes, versions) is the same for every file, so it lives here as
///     constants and static rules. On top of it sits the stored form of one container: the head (magic,
///     version and the head half of the language key), the block of strings, the block of keys, the string
///     buffer and the tail half of the language key. The entries are the container's own
///     (<see cref="W3StringEntry" />, <see cref="W3KeyEntry" />); this class only says where they sit and
///     how they are stored. <see cref="W3StringsReader" /> and <see cref="W3StringsWriter" /> hold no layout
///     of their own.
/// </remarks>
internal sealed class W3StringsStorage
{
    /// <summary>
    ///     The smallest size a file can have and still hold a container: the head, the three section counts
    ///     (one byte at the least) and the tail half of the language key.
    /// </summary>
    private const int MinSize = 15;

    /// <summary>
    ///     The version of the UTF-8 generation: one byte per character, so offsets and lengths count bytes.
    /// </summary>
    public const uint FirstUtf8Version = 164;

    /// <summary>
    ///     The version the classic UTF-16LE generation is written with: two bytes per character, so offsets
    ///     and lengths count characters.
    /// </summary>
    public const uint Utf16LeVersion = 162;

    /// <summary>
    ///     The size of the first block of entries: 12 bytes.
    /// </summary>
    private const int Block1EntrySize = 12;

    /// <summary>
    ///     The size of the second block of entries: 8 bytes.
    /// </summary>
    private const int Block2EntrySize = 8;

    /// <summary>
    ///     The offset the string buffer was found at, which a container laid out for writing does not record.
    /// </summary>
    private long? bufferStart;

    /// <summary>
    ///     The bytes a container holds before its string buffer, built the first time they are asked for.
    /// </summary>
    private byte[]? preamble;

    /// <summary>
    ///     A storage model is built either by reading a container or by laying one out, the two places the
    ///     layout is known.
    /// </summary>
    private W3StringsStorage()
    {
    }

    /// <summary>
    ///     The magic bytes of the container: "RTSW".
    /// </summary>
    private static ReadOnlySpan<byte> MagicBytes => "RTSW"u8;

    /// <summary>
    ///     The strings of the container, in the order the container holds them.
    /// </summary>
    public List<W3StringEntry> Strings { get; private init; } = [];

    /// <summary>
    ///     The keys of the container, in the order the container holds them.
    /// </summary>
    /// <remarks>
    ///     Entries whose hash is zero are not written: zero is how the format says "no key".
    /// </remarks>
    public List<W3KeyEntry> Keys { get; private init; } = [];

    /// <summary>
    ///     The version the texts are stored in.
    /// </summary>
    /// <remarks>
    ///     A read container holds the version measured against the file rather than the one it declares;
    ///     one laid out for writing holds the version it was asked for.
    /// </remarks>
    public uint Version { get; private init; }

    /// <summary>
    ///     The full 32-bit language key of the container.
    /// </summary>
    /// <remarks>
    ///     Stored in two halves, the head one in the header and the tail one in its last two bytes. The
    ///     magic every id and text is obfuscated with follows from it.
    /// </remarks>
    public uint Key { get; private init; }

    /// <summary>
    ///     The magic every id and text of the container is obfuscated with.
    /// </summary>
    private uint Magic { get; init; }

    /// <summary>
    ///     The number of bytes one unit takes in the version the texts are stored in: one for UTF-8, two for
    ///     UTF-16LE.
    /// </summary>
    private int Unit => OffsetUnitSize(Version);

    /// <summary>
    ///     The size the string buffer is stored with, in units.
    /// </summary>
    /// <remarks>
    ///     A container laid out for writing gives it the size the texts and their terminators take; one that
    ///     was read gives the size the file really holds.
    /// </remarks>
    private uint BufferUnits { get; init; }

    /// <summary>
    ///     The offset the string buffer starts at, in bytes.
    /// </summary>
    public long BufferOffset => bufferStart ?? Preamble.Length;

    /// <summary>
    ///     The bytes a container holds before its string buffer: the head, the two blocks of entries and the
    ///     size of the buffer.
    /// </summary>
    /// <remarks>
    ///     The texts themselves are not part of them, so a container is never held whole in memory.
    /// </remarks>
    public byte[] Preamble => preamble ??= BuildPreamble();

    /// <summary>
    ///     The two bytes of the tail half of the language key, which close a container.
    /// </summary>
    public byte[] Tail => [(byte)Key, (byte)(Key >> 8)];

    /// <summary>
    ///     Gets the magic a container of the given language key obfuscates its ids and texts with.
    /// </summary>
    /// <param name="key">The full 32-bit language key, both halves together.</param>
    /// <returns>The magic of the language that owns the key, or zero for a key no language owns.</returns>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows.</exception>
    /// <remarks>
    ///     Every language the game added after release shares key 0, which identifies no language but is
    ///     not an error either: those containers are stored without obfuscation. Any other unknown key
    ///     means the texts were obfuscated with a magic this build does not know.
    /// </remarks>
    public static uint MagicOf(uint key)
    {
        var language = Language.FromKey(key);
        if (language is not null) return language.Magic;
        if (key == 0) return 0;
        throw new W3StringsException(
            $"unknown language key 0x{key:X8}, so the stored texts cannot be decoded " +
            "(the language is not supported by this build)");
    }

    /// <summary>
    ///     The size of the offset unit in bytes: 1 for the UTF-8 generation, 2 for the UTF-16LE generation.
    /// </summary>
    /// <param name="version">The version of the container.</param>
    /// <returns>The size of the offset unit in bytes.</returns>
    private static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }

    /// <summary>
    ///     Gets the version a container asked to be written with a version is written as.
    /// </summary>
    /// <param name="version">The version the container is asked to be written with.</param>
    /// <returns>The version the container is written as.</returns>
    /// <remarks>
    ///     Every version below the UTF-8 generation is written as 162, the single version that generation is
    ///     described by. The UTF-8 generation is written with the version it is asked for.
    /// </remarks>
    private static uint WrittenVersion(uint version)
    {
        return version >= FirstUtf8Version ? version : Utf16LeVersion;
    }

    /// <summary>
    ///     Reads the stored form of a container out of a stream.
    /// </summary>
    /// <param name="input">The stream to read from.</param>
    /// <returns>The stored form of the container, with the version its texts are really stored in.</returns>
    /// <exception cref="W3StringsException">Thrown when the stream does not hold a container this build can decode.</exception>
    /// <remarks>
    ///     The first block points into a buffer that follows it, and the tail half of the language key sits
    ///     in the last two bytes, so the stream is read by offset. The stored bytes of the texts are not read
    ///     here: only the slot of each is recorded.
    /// </remarks>
    public static W3StringsStorage Read(Stream input)
    {
        var length = input.Length;
        if (length < MinSize)
            throw new W3StringsException($"file too small to be a w3strings container ({length} byte(s))");

        using var reader = new BinaryReader(input, Encoding.UTF8, true); // The stream stays open for the caller

        // The magic is compared as it is stored, so the error can quote what was actually found.
        var magic = reader.ReadBytes(MagicBytes.Length);
        if (!magic.AsSpan().SequenceEqual(MagicBytes))
            throw new W3StringsException($"bad magic: expected \"RTSW\", got \"{Encoding.ASCII.GetString(magic)}\"");

        var declared = reader.ReadUInt32(); // The version the container declares
        var key1 = (uint)reader.ReadUInt16() << 16; // The head half: the tail half closes the container
        var key2Offset = length - 2; // The offset the tail half starts at, which no section may reach into

        // The layout of the stored bytes is needed before any text can be decoded.
        var entries = ReadStringEntries(reader, key2Offset);
        var keys = ReadKeys(reader, key2Offset);

        var units = SectionCount.Read(input).Value; // The size the container gives its string buffer
        var start = input.Position; // Where the buffer begins
        var version = StoredVersion(declared, start, units, key2Offset); // The version it is really stored in
        var held = FitBuffer(start, units, version, key2Offset); // The size of the buffer the file holds
        entries = KeepEntriesInBuffer(entries, held, version); // The entries whose text the file holds

        reader.BaseStream.Seek(key2Offset, SeekOrigin.Begin);
        var key = key1 | reader.ReadUInt16();

        return new W3StringsStorage
        {
            Version = version,
            Key = key,
            Magic = MagicOf(key),
            BufferUnits = held,
            bufferStart = start,
            Strings = entries,
            Keys = keys
        };
    }

    /// <summary>
    ///     Lays the stored form of a container out for writing.
    /// </summary>
    /// <param name="file">The container to lay out, whose entries the slots are recorded on.</param>
    /// <param name="version">The version to write it with.</param>
    /// <returns>The stored form of the container, ready to be written out.</returns>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows.</exception>
    /// <remarks>
    ///     The texts are laid out one behind the other, each closed by its terminator, so the buffer holds
    ///     exactly what the texts need. The block of offsets and lengths sits before the buffer, which is
    ///     why the whole layout is known before the first byte is written.
    /// </remarks>
    public static W3StringsStorage Of(W3StringsFile file, uint version)
    {
        var written = WrittenVersion(version); // The older generation is always written as 162
        var unit = OffsetUnitSize(written); // The bytes one character takes in that version

        uint cursor = 0;
        foreach (var entry in file.Strings)
        {
            entry.Length = (uint)StoredText.EncodedLength(entry.Value, unit);
            entry.Offset = cursor;
            cursor += entry.Length + 1; // The terminator that closes the text
        }

        return new W3StringsStorage
        {
            Version = written,
            Key = file.Key,
            Magic = file.Magic, // Every id and text is obfuscated with it
            BufferUnits = cursor, // The buffer holds exactly what the texts and their terminators take
            Strings = file.Strings,
            Keys = file.Keys
        };
    }

    /// <summary>
    ///     The offset the text of an entry is stored at, in bytes.
    /// </summary>
    /// <param name="entry">The string to locate.</param>
    /// <returns>The offset the stored bytes of its text start at, in bytes.</returns>
    public long ByteOffsetOf(W3StringEntry entry)
    {
        return BufferOffset + entry.Offset * Unit;
    }

    /// <summary>
    ///     The size the text of an entry is stored with, in bytes.
    /// </summary>
    /// <param name="entry">The string to measure.</param>
    /// <returns>The size of the stored bytes of its text, in bytes.</returns>
    public int ByteLengthOf(W3StringEntry entry)
    {
        return checked((int)(entry.Length * (uint)Unit));
    }

    /// <summary>
    ///     Reads the text of an entry out of the bytes a container stores it as.
    /// </summary>
    /// <param name="entry">The string the text belongs to, which says how long it is.</param>
    /// <param name="stored">The stored bytes of the text, exactly as the container holds them.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the stored bytes do not hold exactly the characters the entry says the text has.
    /// </exception>
    /// <remarks>
    ///     The bytes are decoded on a copy, so the stored bytes stay as they were read.
    /// </remarks>
    public string TextOf(W3StringEntry entry, byte[] stored)
    {
        return StoredText.FromBytes(stored, (int)entry.Length, Unit).Decode(Magic, Unit);
    }

    /// <summary>
    ///     The id a string is stored under, decoded.
    /// </summary>
    /// <param name="entry">The string whose id is decoded.</param>
    /// <returns>The id the string is stored under.</returns>
    public uint IdOf(W3StringEntry entry)
    {
        return entry.Id ^ Magic;
    }

    /// <summary>
    ///     The id a key resolves to, decoded.
    /// </summary>
    /// <param name="key">The key whose id is decoded.</param>
    /// <returns>The id the key resolves to.</returns>
    public uint IdOf(W3KeyEntry key)
    {
        return key.Id ^ Magic;
    }

    /// <summary>
    ///     The bytes one text occupies in the string buffer: the stored bytes of the text and the terminator
    ///     that closes it.
    /// </summary>
    /// <param name="text">The text to store.</param>
    /// <returns>The bytes the text takes in the buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the version counts in neither one nor two bytes.</exception>
    public byte[] BufferBytesOf(string text)
    {
        var stored = StoredText.From(text, Magic, Unit).Bytes;
        var bytes = new byte[stored.Length + Unit]; // The rest is the terminator, already zero
        stored.CopyTo(bytes, 0);
        return bytes;
    }

    /// <summary>
    ///     Builds the bytes a container holds before its string buffer, every section in the order the format
    ///     prescribes.
    /// </summary>
    /// <returns>The head, both blocks and the size of the string buffer, one after the other.</returns>
    /// <remarks>
    ///     The game resolves both blocks by binary search in the form they are stored in, so each block is
    ///     written in ascending order of the value it holds: the keys by key hash, the strings by id. Because
    ///     the id is stored obfuscated, ordering by clear id is a different order, and a block in the wrong
    ///     order makes the game show the raw "##key" instead of the text (though it still loads).
    /// </remarks>
    private byte[] BuildPreamble()
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);

        writer.Write(MagicBytes);
        writer.Write(Version);
        writer.Write((ushort)(Key >> 16)); // The head half of the language key

        WriteCount(writer, (uint)Strings.Count);
        foreach (var entry in Strings.OrderBy(entry => entry.Id ^ Magic))
        {
            writer.Write(entry.Id ^ Magic); // The id as the container stores it
            writer.Write(entry.Offset);
            writer.Write(entry.Length);
        }

        // A key entry whose hash is zero is not written: zero is how the format says "no key".
        var keys = Keys.Where(key => key.KeyHash != 0).OrderBy(key => key.KeyHash).ToList();
        WriteCount(writer, (uint)keys.Count);
        foreach (var key in keys)
        {
            writer.Write(key.KeyHash);
            writer.Write(key.Id ^ Magic); // The id as the container stores it
        }

        WriteCount(writer, BufferUnits);
        writer.Flush();
        return buffer.ToArray();
    }

    /// <summary>
    ///     Reads the first block: where every text sits in the string buffer.
    /// </summary>
    /// <param name="reader">The reader the block comes from.</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at.</param>
    /// <returns>One entry per text, with the id still obfuscated and the text not yet decoded.</returns>
    private static List<W3StringEntry> ReadStringEntries(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream).Value,
            Block1EntrySize, key2Offset, "block1");

        var entries = new List<W3StringEntry>(count);
        for (var i = 0; i < count; i++)
            entries.Add(new W3StringEntry
            {
                Id = reader.ReadUInt32(),
                Offset = reader.ReadUInt32(),
                Length = reader.ReadUInt32()
            });

        return entries;
    }

    /// <summary>
    ///     Reads the second block: which key resolves to which id.
    /// </summary>
    /// <param name="reader">The reader the block comes from.</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at.</param>
    /// <returns>One entry per key, whose id is still obfuscated.</returns>
    private static List<W3KeyEntry> ReadKeys(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream).Value,
            Block2EntrySize, key2Offset, "block2");

        var keys = new List<W3KeyEntry>(count);
        for (var i = 0; i < count; i++)
            keys.Add(new W3KeyEntry
            {
                KeyHash = reader.ReadUInt32(),
                Id = reader.ReadUInt32()
            });

        return keys;
    }

    /// <summary>
    ///     Works out the version a container is really stored in.
    /// </summary>
    /// <param name="version">The version the container declares.</param>
    /// <param name="start">The offset the string buffer starts at.</param>
    /// <param name="units">The size the container gives its string buffer, in units.</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at.</param>
    /// <returns>The version the container is stored in.</returns>
    /// <remarks>
    ///     Some community containers declare the older version while storing UTF-8, so the declaration is
    ///     measured against the file: a container whose buffer only ends where the language key begins when
    ///     counted in bytes holds UTF-8 whatever it declares. The correction is logged.
    /// </remarks>
    private static uint StoredVersion(uint version, long start, uint units, long key2Offset)
    {
        var declared = OffsetUnitSize(version);
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
            version, FirstUtf8Version);
        return FirstUtf8Version;
    }

    /// <summary>
    ///     Answers the version a container of the older generation is read as.
    /// </summary>
    /// <param name="version">The version the container declares.</param>
    /// <returns>The version 162, the single layout the older generation has.</returns>
    /// <remarks>
    ///     A container that declares another version of that generation, 163 among them, is read as 162 and
    ///     the correction is logged.
    /// </remarks>
    private static uint OlderGeneration(uint version)
    {
        if (version == Utf16LeVersion) return version;

        Log.Warning(
            "The container declares version {Declared}, which is read as version {Stored}: the older " +
            "generation of the game has one layout",
            version, Utf16LeVersion);
        return Utf16LeVersion;
    }

    /// <summary>
    ///     Answers the size of the string buffer the file really holds.
    /// </summary>
    /// <param name="start">The offset the string buffer starts at.</param>
    /// <param name="units">The size the container gives its string buffer, in units.</param>
    /// <param name="version">The version the container is stored in.</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at.</param>
    /// <returns>The size of the buffer, in units.</returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when the buffer reaches past the end of the file and the container are not one to read up
    ///     to what the file holds.
    /// </exception>
    /// <remarks>
    ///     A UTF-8 container that declares a larger buffer than the file holds is read up to the end of the
    ///     file with a warning; the older generation refuses it instead.
    /// </remarks>
    private static uint FitBuffer(long start, uint units, uint version, long key2Offset)
    {
        var unit = OffsetUnitSize(version);
        if (start + units * unit <= key2Offset) return units; // The buffer fits what the file holds

        if (version < FirstUtf8Version)
            throw new W3StringsException(
                $"string buffer overruns file ({start + units * unit} > {key2Offset})");

        var held = (uint)(Math.Max(0, key2Offset - start) / unit); // How many units of the buffer the file has
        Log.Warning(
            "The container declares a string buffer of {Declared} unit(s), of which the file holds {HeldUnits}: " +
            "the buffer is read up to the end of the file",
            units, held);
        return held;
    }

    /// <summary>
    ///     Keeps the entries whose text the string buffer holds.
    /// </summary>
    /// <param name="entries">The entries of the first block, in entry order.</param>
    /// <param name="units">The size of the string buffer the file holds, in units.</param>
    /// <param name="version">The version the container is stored in.</param>
    /// <returns>The entries whose text lies inside the buffer, in entry order.</returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when an entry points outside the buffer of a container that is not read up to what the file
    ///     holds.
    /// </exception>
    /// <remarks>
    ///     The older generation has two readings, so a text outside the buffer is not one to drop: the file
    ///     is refused instead. The UTF-8 generation has a single reading, so such entries are dropped with a
    ///     warning.
    /// </remarks>
    private static List<W3StringEntry> KeepEntriesInBuffer(List<W3StringEntry> entries, uint units, uint version)
    {
        var outside = entries.FindIndex(entry => (long)entry.Offset + entry.Length > units);
        if (outside < 0) return entries; // Every text is inside the buffer, which is what a container holds

        if (version < FirstUtf8Version)
            throw new W3StringsException(
                $"string entry #{outside} points outside the string buffer " +
                $"(offset {entries[outside].Offset}, length {entries[outside].Length}, buffer {units} unit(s))");

        var kept = entries.Where(entry => (long)entry.Offset + entry.Length <= units).ToList();
        Log.Warning(
            "{Dropped} of {Declared} string entries reach past the {Units} unit(s) of string buffer the file " +
            "holds: they are dropped",
            entries.Count - kept.Count, entries.Count, units);
        return kept;
    }

    /// <summary>
    ///     Checks that a block of fixed-size entries fits the file, and answers how many there are.
    /// </summary>
    /// <param name="start">The offset the block starts at.</param>
    /// <param name="count">The number of entries the container declares.</param>
    /// <param name="entrySize">The size of one entry in bytes.</param>
    /// <param name="key2Offset">The offset the language key at the end of the file starts at.</param>
    /// <param name="name">The name of the block, used in the error message.</param>
    /// <returns>The number of entries, as an index.</returns>
    /// <exception cref="W3StringsException">Thrown when the block does not fit before the end of the file.</exception>
    /// <remarks>
    ///     Unlike the string buffer, a block is not read up to what the file holds: every later section
    ///     starts where the block ends, so a count that does not fit makes the file refused rather than
    ///     guessed at.
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
    ///     Writes a count the way a container stores it.
    /// </summary>
    /// <param name="writer">The writer the count goes to.</param>
    /// <param name="value">The value to write as a count.</param>
    private static void WriteCount(BinaryWriter writer, uint value)
    {
        writer.Write(SectionCount.Of(value).Bytes);
    }
}