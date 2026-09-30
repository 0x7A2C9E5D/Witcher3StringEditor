using System.Text;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Reads a w3strings container out of a stream
///     The container is read section by section and never held whole: the first block says where every
///     text sits, so the payloads are fetched one at a time and only the model keeps them
/// </summary>
public static class W3StringsReader
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

            // The layout of the payloads is needed before any text can be decoded.
            var entries = ReadStringEntries(reader, key2Offset);
            var keys = ReadKeys(reader, key2Offset);
            var buffer = ReadBuffer(reader, key2Offset, head.Unit);

            var payloads = ReadPayloads(input, entries, buffer, head.Unit);

            // The key closes the container with its tail half, and it is what the magic every id and
            // payload was obfuscated with follows from.
            var key = head.Key | ReadKey2(reader, key2Offset);
            var magic = W3StringsFormat.MagicOf(key);

            return ToFile(head with { Key = key }, magic, entries, keys, payloads, head.Unit);
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
    /// <returns>The head of the container</returns>
    /// <exception cref="W3StringsException">Thrown when the stream is too short, or is not a container at all</exception>
    private static Head ReadHead(BinaryReader reader, long length)
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
        return new Head(version, key, W3StringsFormat.OffsetUnitSize(version));
    }

    /// <summary>
    ///     Reads the first block: where every text sits in the string buffer
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per text, without its payload</returns>
    private static StringEntry[] ReadStringEntries(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, VariableLengthCodec.Read(reader.BaseStream),
            W3StringsFormat.Block1EntrySize, key2Offset, "block1");

        var entries = new StringEntry[count];
        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var length = reader.ReadUInt32();
            entries[i] = new StringEntry(id, offset, length);
        }

        return entries;
    }

    /// <summary>
    ///     Reads the second block: which key resolves to which id
    /// </summary>
    /// <param name="reader">The reader the block comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <returns>One entry per key, still obfuscated</returns>
    private static KeyEntry[] ReadKeys(BinaryReader reader, long key2Offset)
    {
        var count = CheckBlockFits(reader.BaseStream.Position, VariableLengthCodec.Read(reader.BaseStream),
            W3StringsFormat.Block2EntrySize, key2Offset, "block2");

        var keys = new KeyEntry[count];
        for (var i = 0; i < count; i++)
        {
            var hash = reader.ReadUInt32();
            var id = reader.ReadUInt32();
            keys[i] = new KeyEntry(hash, id);
        }

        return keys;
    }

    /// <summary>
    ///     Reads the size the container gives its string buffer, which holds every text of it
    /// </summary>
    /// <param name="reader">The reader the size comes from</param>
    /// <param name="key2Offset">The offset the language key at the end of the container starts at</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>Where the string buffer starts, and how large the container says it is</returns>
    /// <exception cref="W3StringsException">Thrown when the buffer reaches past the end of the file</exception>
    private static Buffer ReadBuffer(BinaryReader reader, long key2Offset, int unit)
    {
        var units = VariableLengthCodec.Read(reader.BaseStream);
        var start = reader.BaseStream.Position;
        var end = start + units * unit;
        return end > key2Offset
            ? throw new W3StringsException($"string buffer overruns file ({end} > {key2Offset})")
            : new Buffer(start, units);
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
    /// <param name="magic">The magic every id and payload was obfuscated with</param>
    /// <param name="entries">The entries of the first block</param>
    /// <param name="keys">The entries of the second block</param>
    /// <param name="payloads">The stored bytes of every entry, in entry order, which decoding consumes</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The container</returns>
    private static W3StringsFile ToFile(Head head, uint magic, StringEntry[] entries, KeyEntry[] keys,
        byte[][] payloads, int unit)
    {
        var file = new W3StringsFile
        {
            Version = head.Version,
            Key = head.Key
        };

        // Only the id and the text survive: where the container put a text and how long it said it was
        // are facts of the encoding, and the writer works both out again for itself.
        for (var i = 0; i < entries.Length; i++)
            file.Strings.Add(new W3StringEntry
            {
                Id = entries[i].Id ^ magic,
                Value = PayloadCodec.Decode(payloads[i], (int)entries[i].Length, magic, unit)
            });

        foreach (var entry in keys)
            file.Keys.Add(new W3KeyEntry { KeyHash = entry.Hash, Id = entry.Id ^ magic });

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
    /// <exception cref="W3StringsException">Thrown when the block does not fit before the payload limit</exception>
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
    ///     Reads the text of every entry out of the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="entries">The entries of the first block, in entry order</param>
    /// <param name="buffer">The extent of the string buffer</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The stored bytes of every entry, in entry order</returns>
    /// <exception cref="W3StringsException">Thrown when an entry points outside the string buffer</exception>
    private static byte[][] ReadPayloads(Stream input, StringEntry[] entries, Buffer buffer, int unit)
    {
        var payloads = new byte[entries.Length][];
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

            var payload = new byte[entries[i].Length * unit];
            input.ReadExactly(payload);
            payloads[i] = payload;
            cursor = at + payload.Length;
        }

        return payloads;
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

    /// <summary>
    ///     The head of a container
    /// </summary>
    /// <param name="Version">The version the container was written with</param>
    /// <param name="Key">The language key with its head half in place, waiting for the tail half</param>
    /// <param name="Unit">The number of bytes one character takes in the container</param>
    private readonly record struct Head(uint Version, uint Key, int Unit);

    /// <summary>
    ///     The string buffer the entries of a container are read out of
    /// </summary>
    /// <param name="Start">The offset the buffer starts at, in bytes</param>
    /// <param name="Units">The size the container gives the buffer, in units</param>
    /// <remarks>
    ///     The size is what holds every entry inside the buffer, and it is dropped as soon as the texts
    ///     have been read: the writer works the size of the buffer out from the texts it lays out in it
    /// </remarks>
    private readonly record struct Buffer(long Start, uint Units);

    /// <summary>
    ///     One entry of the first block, as it is stored
    /// </summary>
    /// <param name="Id">The id of the entry, still obfuscated</param>
    /// <param name="Offset">The offset of the text, in units</param>
    /// <param name="Length">The length of the text, in units</param>
    private readonly record struct StringEntry(uint Id, uint Offset, uint Length);

    /// <summary>
    ///     One entry of the second block, as it is stored
    /// </summary>
    /// <param name="Hash">The hash of the key</param>
    /// <param name="Id">The id of the string the key resolves to, still obfuscated</param>
    private readonly record struct KeyEntry(uint Hash, uint Id);
}