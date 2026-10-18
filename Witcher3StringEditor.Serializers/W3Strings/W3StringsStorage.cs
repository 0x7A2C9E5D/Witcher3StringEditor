using System.Text;
using Serilog;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The storage model of the w3strings format: the facts the format fixes for every file, and the stored
///     form of one container
/// </summary>
/// <remarks>
///     <para>
///         The format itself comes first: its magic, the size of its sections, the versions it is written in
///         and what each of them means. Those are the same for every file, so they are constants and static
///         rules of this class rather than facts of the container being read or written.
///     </para>
///     <para>
///         On top of them sits the stored form of one container, which is the one place the layout of a file
///         is written down: the head (the magic, the version and the head half of the language key), the
///         block of strings, the block of keys, the string buffer, and the tail half of the language key
///         that closes the file.
///     </para>
///     <para>
///         The entries themselves are the container's own: the strings are <see cref="W3StringEntry" /> and
///         the keys are <see cref="W3KeyEntry" />, so a string or a key is defined once and answered for in
///         both directions. What belongs to this class is where they sit and how they are stored: an id is
///         obfuscated with the magic of the language, an offset and a length are counted in the units the
///         version counts in, and a text is a run of obfuscated bytes of its encoding.
///     </para>
///     <para>
///         It is built one of two ways. <see cref="Read" /> reads a container off a stream, and works out
///         the layout of the bytes the file really holds: the version the texts are stored in is measured
///         against the file rather than taken from what it declares, and a buffer that reaches past the end
///         of the file is read up to what is there. <see cref="Of" /> lays a container out for writing, so
///         the slot of every text is recorded as the buffer is laid out, and both blocks are ordered the way
///         the game searches them.
///     </para>
///     <para>
///         The reading and writing classes hold no layout of their own: <see cref="W3StringsReader" /> reads
///         this model off a stream and fetches the stored bytes of every text from where it says they sit,
///         and <see cref="W3StringsWriter" /> writes the bytes it lays down in the order it holds them.
///     </para>
/// </remarks>
internal sealed class W3StringsStorage
{
    /// <summary>
    ///     The size a file has to reach before it can hold a container at all: the head (the magic, the
    ///     version and the head half of the language key), the count that introduces each of the three
    ///     sections, one byte at the least, and the tail half of the language key that closes it
    /// </summary>
    /// <remarks>
    ///     The smallest container, the one that holds no entry at all, is exactly this size: writing one has
    ///     to leave a file this model can read back
    /// </remarks>
    private const int MinSize = 15;

    /// <summary>
    ///     The version of the UTF-8 generation: one byte per character, so every offset and length counts
    ///     bytes. Every version from this one on is read that way
    /// </summary>
    public const uint FirstUtf8Version = 164;

    /// <summary>
    ///     The version the classic UTF-16LE generation is written with: two bytes per character, so every
    ///     offset and length counts characters
    /// </summary>
    /// <remarks>
    ///     It is the version a save writes for that generation, and the one every version below the UTF-8
    ///     generation is read as: a container of the game declares 162 or 163, and both count their offsets,
    ///     lengths and buffer size in characters of two bytes
    /// </remarks>
    public const uint Utf16LeVersion = 162;

    /// <summary>
    ///     The size of the first block of entries: 12 bytes
    /// </summary>
    private const int Block1EntrySize = 12;

    /// <summary>
    ///     The size of the second block of entries: 8 bytes
    /// </summary>
    private const int Block2EntrySize = 8;

    /// <summary>
    ///     The magic bytes of the container: "RTSW"
    /// </summary>
    private static ReadOnlySpan<byte> MagicBytes => "RTSW"u8;

    /// <summary>
    ///     Gets the magic a container of the given language key obfuscates its ids and texts with
    /// </summary>
    /// <param name="key">The full 32-bit language key, both halves together</param>
    /// <returns>The magic of the language that owns the key, or zero for a key no language owns</returns>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows</exception>
    /// <remarks>
    ///     Every language the game added after its release shares key 0, so a zero key identifies no
    ///     language but is not an error either: those containers are stored without obfuscation. Any
    ///     other key that owns no language means the texts were obfuscated with a magic this build does
    ///     not know, and decoding it as if it were cleartext would turn every string into garbage
    ///     without a single error, which is worse than refusing the file
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
    ///     The size of the offset unit in bytes: 1 byte for a container of the UTF-8 generation, 2 bytes for
    ///     one of the UTF-16LE generation
    /// </summary>
    /// <param name="version">The version of the container</param>
    /// <returns>The size of the offset unit in bytes</returns>
    /// <remarks>
    ///     Everything below the UTF-8 generation is counted in characters of two bytes, so 162 and 163 are
    ///     answered for alike: the older generation of the game is one layout, and the version a container of
    ///     it declares is an upper bound rather than a difference. The version a container is really stored
    ///     in is worked out when it is read, which is the one it declares unless it declares one generation
    ///     and holds the other: see <see cref="StoredVersion" />
    /// </remarks>
    private static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }

    /// <summary>
    ///     Gets the version a container asked to be written with a version is written as
    /// </summary>
    /// <param name="version">The version the container is asked to be written with</param>
    /// <returns>The version the container is written as</returns>
    /// <remarks>
    ///     The older generation of the game has one layout, which is the one version 162 describes, so every
    ///     version below the UTF-8 generation is written as 162: a file that declared another version of that
    ///     generation, 163 among them, would only make a reader guess at a difference that is not there. The
    ///     UTF-8 generation is written with the version it is asked for
    /// </remarks>
    private static uint WrittenVersion(uint version)
    {
        return version >= FirstUtf8Version ? version : Utf16LeVersion;
    }

    /// <summary>
    ///     The offset the string buffer was found at, which a container laid out for writing does not
    ///     record: its buffer starts where the bytes before it leave off
    /// </summary>
    private long? bufferStart;

    /// <summary>
    ///     The bytes a container holds before its string buffer, built the first time they are asked for
    /// </summary>
    private byte[]? preamble;

    /// <summary>
    ///     A storage model is built by reading a container or by laying one out, which are the two places
    ///     the facts of the layout are known
    /// </summary>
    private W3StringsStorage()
    {
    }

    /// <summary>
    ///     The strings of the container, in the order the container holds them
    /// </summary>
    /// <remarks>
    ///     A container that was read holds the entries the file holds, with the ids still obfuscated and no
    ///     text decoded yet; one laid out for writing holds the entries of the container itself, in the order
    ///     the model lists them, which is the order their texts are laid out in the buffer
    /// </remarks>
    public List<W3StringEntry> Strings { get; private init; } = [];

    /// <summary>
    ///     The keys of the container, in the order the container holds them
    /// </summary>
    /// <remarks>
    ///     A container laid out for writing holds the entries of the model itself, and the ones whose hash
    ///     is zero are not written: zero is how the format says "no key", so such an entry is one no key can
    ///     ever resolve to
    /// </remarks>
    public List<W3KeyEntry> Keys { get; private init; } = [];

    /// <summary>
    ///     The version the texts are stored in
    /// </summary>
    /// <remarks>
    ///     A container that was read is stored in the version that was measured against it rather than in
    ///     the one it declared, which are the same except for a container that declares one generation and
    ///     holds the other; one laid out for writing is stored in the version it was asked for, with the
    ///     older generation written as the version 162 describes it
    /// </remarks>
    public uint Version { get; private init; }

    /// <summary>
    ///     The full 32-bit language key of the container
    /// </summary>
    /// <remarks>
    ///     The container stores it in two halves: the head one in its header, the tail one in It's very last
    ///     two bytes. It is what identifies the language, and the magic every id and text is obfuscated
    ///     with follows from it
    /// </remarks>
    public uint Key { get; private init; }

    /// <summary>
    ///     The magic every id and text of the container is obfuscated with
    /// </summary>
    private uint Magic { get; init; }

    /// <summary>
    ///     The number of bytes one unit takes in the version the texts are stored in: one for UTF-8, two for
    ///     UTF-16LE
    /// </summary>
    private int Unit => OffsetUnitSize(Version);

    /// <summary>
    ///     The size the string buffer is stored with, in units
    /// </summary>
    /// <remarks>
    ///     A container laid out for writing gives it the size the texts and their terminators take, so it
    ///     holds nothing else; one that was read gives it the size the file really holds, which is the
    ///     declared one unless the buffer reaches past the end of the file
    /// </remarks>
    private uint BufferUnits { get; init; }

    /// <summary>
    ///     The offset the string buffer starts at, in bytes
    /// </summary>
    /// <remarks>
    ///     A container that was read records where the buffer was found; one laid out for writing starts it
    ///     where the bytes it holds before it leave off
    /// </remarks>
    public long BufferOffset => bufferStart ?? Preamble.Length;

    /// <summary>
    ///     The bytes a container holds before its string buffer: the head, the two blocks of entries and the
    ///     size of the buffer, which the format lays out in that order
    /// </summary>
    /// <remarks>
    ///     The texts themselves are not part of them: the buffer is streamed one text at a time, so a
    ///     container is never held whole in memory
    /// </remarks>
    public byte[] Preamble => preamble ??= BuildPreamble();

    /// <summary>
    ///     The two bytes of the tail half of the language key, which close a container
    /// </summary>
    public byte[] Tail => [(byte)Key, (byte)(Key >> 8)];

    /// <summary>
    ///     Reads the stored form of a container out of a stream
    /// </summary>
    /// <param name="input">The stream to read from, which a caller opens over a file</param>
    /// <returns>The stored form of the container, and the version its texts are really stored in</returns>
    /// <exception cref="W3StringsException">Thrown when the stream does not hold a container this build can decode</exception>
    /// <remarks>
    ///     The offsets of the first block point into a buffer that follows it, and the tail half of the
    ///     language key sits in the last two bytes of the container. Reaching both out of order is why the
    ///     stream is read by offset: over a file that is what a stream is there for. The stored bytes of the
    ///     texts themselves are not read here: the slot of every one of them is recorded, and a reader
    ///     fetches them one at a time
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

        var units = SectionCount.Read(input); // The size the container gives its string buffer
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
    ///     Lays the stored form of a container out for writing
    /// </summary>
    /// <param name="file">The container to lay out, whose entries the slots are recorded on</param>
    /// <param name="version">The version to write it with, which the caller chose for the game it is for</param>
    /// <returns>The stored form of the container, ready to be written out</returns>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows</exception>
    /// <remarks>
    ///     The texts are laid out one behind the other in the order the container lists them, each closed by
    ///     the terminator the format asks for, so the slot is recorded as the buffer is laid out and the
    ///     buffer holds exactly what the texts need. The block of offsets and lengths sits before the buffer,
    ///     which is why the whole layout is known before the first byte is written
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
    ///     The offset the text of an entry is stored at, in bytes
    /// </summary>
    /// <param name="entry">The string to locate</param>
    /// <returns>The offset the stored bytes of its text start at, in bytes</returns>
    public long ByteOffsetOf(W3StringEntry entry)
    {
        return BufferOffset + entry.Offset * Unit;
    }

    /// <summary>
    ///     The size the text of an entry is stored with, in bytes
    /// </summary>
    /// <param name="entry">The string to measure</param>
    /// <returns>The size of the stored bytes of its text, in bytes</returns>
    public int ByteLengthOf(W3StringEntry entry)
    {
        return checked((int)(entry.Length * (uint)Unit));
    }

    /// <summary>
    ///     Reads the text of an entry out of the bytes a container stores it as
    /// </summary>
    /// <param name="entry">The string the text belongs to, which says how long it is</param>
    /// <param name="stored">The stored bytes of the text, exactly as the container holds them</param>
    /// <returns>The text</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the stored bytes do not hold exactly the characters the entry says the text has
    /// </exception>
    /// <remarks>
    ///     Deobfuscating changes the bytes in place, so the stored bytes are decoded on a copy and stay as
    ///     they were read
    /// </remarks>
    public string TextOf(W3StringEntry entry, byte[] stored)
    {
        return StoredText.FromBytes(stored, (int)entry.Length, Unit).Decode(Magic, Unit);
    }

    /// <summary>
    ///     The id a string is stored under, decoded
    /// </summary>
    /// <param name="entry">The string whose id is decoded</param>
    /// <returns>The id the string is stored under</returns>
    public uint IdOf(W3StringEntry entry)
    {
        return entry.Id ^ Magic;
    }

    /// <summary>
    ///     The id a key resolves to, decoded
    /// </summary>
    /// <param name="key">The key whose id is decoded</param>
    /// <returns>The id the key resolves to</returns>
    public uint IdOf(W3KeyEntry key)
    {
        return key.Id ^ Magic;
    }

    /// <summary>
    ///     The bytes one text occupies in the string buffer: the stored bytes of the text and the
    ///     terminator that closes it
    /// </summary>
    /// <param name="text">The text to store</param>
    /// <returns>The bytes the text takes in the buffer</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the version counts in neither one nor two bytes</exception>
    public byte[] BufferBytesOf(string text)
    {
        var stored = StoredText.From(text, Magic, Unit).Bytes;
        var bytes = new byte[stored.Length + Unit]; // The rest is the terminator, already zero
        stored.CopyTo(bytes, 0);
        return bytes;
    }

    /// <summary>
    ///     Builds the bytes a container holds before its string buffer, every section in the order the
    ///     format prescribes
    /// </summary>
    /// <returns>The head, both blocks and the size of the string buffer, one after the other</returns>
    /// <remarks>
    ///     The ids are obfuscated as they are written, so this is the layout of a container that is about to
    ///     be written rather than one that was read. The game resolves both blocks by binary search, and it
    ///     searches them in the form they are stored in: the block of keys by localization-key hash, which
    ///     is stored as it is, and the block of strings by string id, which is stored obfuscated. Each block
    ///     is therefore written in ascending order of the value it holds. Ordering the strings by their
    ///     clear ids is not the same order, because the obfuscating xor does not keep the order of the
    ///     values it is applied to: every language that has a magic then stops resolving, while the
    ///     containers of the languages without one keep working, and the game shows the raw "##key" it asked
    ///     for instead of the text. A block in the wrong order still loads without an error.
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

        // A key entry whose hash is zero is not written: zero is how the format says "no key", so such an
        // entry is one no key can ever resolve to, and a container that holds one holds an entry the game
        // can do nothing with.
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
    ///     Reads the first block: where every text sits in the string buffer
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per text, with the id still obfuscated and the text not yet decoded</returns>
    private static List<W3StringEntry> ReadStringEntries(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream),
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
    ///     Reads the second block: which key resolves to which id
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per key, whose id is still obfuscated</returns>
    private static List<W3KeyEntry> ReadKeys(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, SectionCount.Read(reader.BaseStream),
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
        if (version == Utf16LeVersion) return version;

        Log.Warning(
            "The container declares version {Declared}, which is read as version {Stored}: the older " +
            "generation of the game has one layout",
            version, Utf16LeVersion);
        return Utf16LeVersion;
    }

    /// <summary>
    ///     Answers the size of the string buffer the file really holds
    /// </summary>
    /// <param name="start">The offset the string buffer starts at</param>
    /// <param name="units">The size the container gives its string buffer, in units</param>
    /// <param name="version">The version the container is stored in</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>The size of the buffer, in units, which is the declared one unless the file holds less</returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when the buffer reaches past the end of the file and the container are not one to read up
    ///     to what the file holds
    /// </exception>
    /// <remarks>
    ///     A container of the UTF-8 generation that declares a larger buffer than the file holds is read up
    ///     to the end of the file with a warning, because that generation has a single reading: what is not
    ///     there cannot be decoded, and what is their stays readable
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
    ///     Keeps the entries whose text the string buffer holds
    /// </summary>
    /// <param name="entries">The entries of the first block, in entry order</param>
    /// <param name="units">The size of the string buffer the file holds, in units</param>
    /// <param name="version">The version the container is stored in</param>
    /// <returns>The entries whose text lies inside the buffer, in entry order</returns>
    /// <exception cref="W3StringsException">
    ///     Thrown when an entry points outside the buffer of a container that is not read up to what the file
    ///     holds
    /// </exception>
    /// <remarks>
    ///     A container of the older generation has two readings, so a text that is not inside the buffer it
    ///     declared is not one to drop: the reading may be the wrong one, and the file is refused instead.
    ///     The UTF-8 generation has a single reading, so an entry the file does not hold is dropped with one
    ///     warning for all of them, which leaves every text that is there readable
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
    ///     Writes a count the way a container stores it
    /// </summary>
    /// <param name="writer">The writer the count goes to</param>
    /// <param name="value">The value to write as a count</param>
    private static void WriteCount(BinaryWriter writer, uint value)
    {
        writer.Write(SectionCount.Write(value));
    }
}
