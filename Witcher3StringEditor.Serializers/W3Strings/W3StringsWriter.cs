using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Encodes a container into a stream
///     The block of offsets and lengths sits before the string buffer, so the slot of every entry has to
///     be known before the first byte is written. The lengths are therefore measured in a first pass,
///     which only needs the size of every text and never the encoded bytes themselves
/// </summary>
internal static class W3StringsWriter
{
    /// <summary>
    ///     Measures every text, records the slot it takes in the string buffer, and answers how large that
    ///     buffer is, encoding nothing
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <returns>The size the entries and their terminators occupy, and the size of the buffer in bytes</returns>
    /// <remarks>
    ///     The texts are laid out one behind the other in the order they are listed, each closed by the
    ///     terminator the format asks for, so the slot is recorded as the buffer is laid out and every entry
    ///     carries the offsets that point at its own text: the order of the block of offsets is then free,
    ///     and the buffer holds exactly what the texts need
    /// </remarks>
    private static (uint Used, long Size) BufferOf(W3StringsFile file)
    {
        var unit = file.Unit;
        uint cursor = 0;

        foreach (var entry in file.Strings)
        {
            entry.Length = (uint)StoredText.EncodedLength(entry.Value, unit);
            entry.Offset = cursor;
            cursor += entry.Length + 1; // The terminator that closes the text
        }

        return (cursor, cursor * (uint)unit);
    }

    /// <summary>
    ///     Writes a container into the given stream, every section in the order the format prescribes
    /// </summary>
    /// <param name="output">The stream the container is written to</param>
    /// <param name="file">The container to encode</param>
    public static void Write(Stream output, W3StringsFile file)
    {
        // The layout has to be known before the first byte is written, because the block of offsets and
        // lengths sits before the string buffer it points into.
        var buffer = BufferOf(file);
        var magic = file.Magic;
        using var writer = new BinaryWriter(output, Encoding.UTF8, true); // The stream stays open for the caller

        writer.Write(W3StringsFormat.MagicBytes);
        writer.Write(file.Version);
        writer.Write(file.Key1);

        // The game resolves both blocks by binary search: entries by string id in the first block, and
        // keys by localization-key hash in the second. Each block is therefore written in ascending
        // order of the value it is searched by. A key block in any other order still loads without an
        // error, but most of its keys stop resolving, and the game shows the menu the raw "##key" it
        // asked for instead of the text.
        WriteCount(writer, (uint)file.Strings.Count);
        foreach (var entry in file.Strings.OrderBy(entry => entry.Id))
        {
            writer.Write(entry.Id ^ magic);
            writer.Write(entry.Offset);
            writer.Write(entry.Length);
        }

        WriteCount(writer, (uint)file.Keys.Count);
        foreach (var key in file.Keys.OrderBy(key => key.KeyHash))
        {
            writer.Write(key.KeyHash);
            writer.Write(key.Id ^ magic);
        }

        WriteCount(writer, (uint)(buffer.Size / file.Unit));
        WriteBuffer(writer, file, buffer);

        writer.Write(file.Key2);
    }

    /// <summary>
    ///     Writes the string buffer: every text, closed by its terminator, and the zeroes the size the
    ///     container gives the buffer leaves over
    /// </summary>
    /// <param name="writer">The writer the buffer goes to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="buffer">The sizes the buffer is written with</param>
    private static void WriteBuffer(BinaryWriter writer, W3StringsFile file, (uint Used, long Size) buffer)
    {
        var unit = file.Unit;
        var magic = file.Magic;

        foreach (var stored in file.Strings.Select(entry => StoredText.Encode(entry.Value, magic, unit, out _)))
        {
            writer.Write(stored);
            WriteZeros(writer, unit); // The terminator that closes the text
        }

        // Whatever the buffer size leaves beyond the entries and their terminators.
        WriteZeros(writer, buffer.Size - buffer.Used * (uint)unit);
    }

    /// <summary>
    ///     Writes a run of zero bytes, which the terminator and the room a larger buffer leaves over
    ///     are made of
    /// </summary>
    /// <param name="writer">The writer the zeroes go to</param>
    /// <param name="count">The number of zero bytes to write</param>
    private static void WriteZeros(BinaryWriter writer, long count)
    {
        if (count <= 0) return;

        var zeros = new byte[(int)Math.Min(count, 4096)];
        while (count > 0)
        {
            var chunk = (int)Math.Min(count, zeros.Length);
            writer.Write(zeros, 0, chunk);
            count -= chunk;
        }
    }

    private static void WriteCount(BinaryWriter writer, uint value)
    {
        writer.Write(SectionCount.Write(value));
    }
}