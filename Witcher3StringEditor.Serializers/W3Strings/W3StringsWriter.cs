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
    ///     Measures every text and places it in the string buffer, encoding nothing
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <returns>The slot of every entry, and the size of the buffer they occupy</returns>
    /// <remarks>
    ///     The texts are laid out one behind the other, each closed by the terminator the format asks
    ///     for, so the buffer holds exactly what they need and nothing more
    /// </remarks>
    private static Buffer BufferOf(W3StringsFile file)
    {
        var unit = file.Unit;
        var lengths = new int[file.Strings.Count];
        var offsets = new uint[file.Strings.Count];
        uint cursor = 0;

        for (var i = 0; i < file.Strings.Count; i++)
        {
            lengths[i] = PayloadCodec.Measure(file.Strings[i].Value, unit);
            offsets[i] = cursor;
            cursor += (uint)lengths[i] + 1;
        }

        return new Buffer(lengths, offsets, cursor, cursor * (uint)unit);
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

        WriteCount(writer, (uint)file.Strings.Count);
        for (var i = 0; i < file.Strings.Count; i++)
        {
            writer.Write(file.Strings[i].Id ^ magic);
            writer.Write(buffer.Offsets[i]);
            writer.Write((uint)buffer.Lengths[i]);
        }

        WriteCount(writer, (uint)file.Keys.Count);
        foreach (var key in file.Keys)
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
    /// <param name="buffer">The slot of every entry, and the sizes the buffer is written with</param>
    private static void WriteBuffer(BinaryWriter writer, W3StringsFile file, Buffer buffer)
    {
        var unit = file.Unit;
        var magic = file.Magic;

        foreach (var payload in file.Strings.Select(t => PayloadCodec.Encode(t.Value, magic, unit, out _)))
        {
            writer.Write(payload);
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
        writer.Write(VariableLengthCodec.Write(value));
    }

    /// <summary>
    ///     Where every text sits in the string buffer, and how large that buffer is
    /// </summary>
    /// <param name="Lengths">The length of every entry, in units</param>
    /// <param name="Offsets">The offset of every entry, in units</param>
    /// <param name="Used">The size the entries and their terminators occupy, in units</param>
    /// <param name="Size">The size of the string buffer, in bytes</param>
    private readonly record struct Buffer(int[] Lengths, uint[] Offsets, uint Used, long Size);
}