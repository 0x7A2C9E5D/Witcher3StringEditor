using System.Text;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Encodes a container into a stream
///     The block of offsets and lengths sits before the string buffer, so the slot of every entry has to
///     be known before the first byte is written. The lengths are therefore measured in a first pass,
///     which only needs the size of every text and never the encoded bytes themselves
/// </summary>
public static class W3StringsWriter
{
    /// <summary>
    ///     Writes a container into the given stream
    /// </summary>
    /// <param name="output">The stream the container is written to</param>
    /// <param name="file">The container to encode</param>
    /// <exception cref="ArgumentNullException">Thrown when the stream or the container is null</exception>
    public static void Write(Stream output, W3StringsFile file)
    {
        WriteContainer(output, file, BufferOf(file));
    }

    /// <summary>
    ///     Measures every text and places it in the string buffer, encoding nothing
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <returns>The slot of every entry, and the size of the buffer they occupy</returns>
    /// <remarks>
    ///     The texts are laid out one behind the other, each closed by the terminator the format asks for
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

        // A container may declare a larger buffer than its entries need, and the room left over is
        // written as zeroes.
        return new Buffer(lengths, offsets, Math.Max(cursor * (uint)unit, file.DeclaredBufferUnits * unit));
    }

    /// <summary>
    ///     Writes every section of the container in the order the format prescribes
    /// </summary>
    /// <param name="output">The stream the container is written to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="buffer">The slot of every entry, and the size of the buffer they occupy</param>
    private static void WriteContainer(Stream output, W3StringsFile file, Buffer buffer)
    {
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

        WriteCount(writer, buffer.Units(file.Unit));
        WriteBuffer(writer, file, buffer);

        if (file.Trailer.Length > 0) writer.Write(file.Trailer);
        writer.Write(file.Key2);
    }

    /// <summary>
    ///     Writes the string buffer: every text, closed by its terminator, and the zeroes the declared
    ///     size leaves over
    /// </summary>
    /// <param name="writer">The writer the buffer goes to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="buffer">The slot of every entry, and the size of the buffer they occupy</param>
    private static void WriteBuffer(BinaryWriter writer, W3StringsFile file, Buffer buffer)
    {
        var unit = file.Unit;
        var magic = file.Magic;

        for (var i = 0; i < file.Strings.Count; i++)
        {
            var payload = PayloadCodec.Encode(file.Strings[i].Value, magic, unit, out _);
            writer.Write(payload);
            WriteZeros(writer, unit); // The terminator that closes the text
        }

        WriteZeros(writer, buffer.Size - buffer.Used(unit)); // Whatever the declared size leaves over
    }

    /// <summary>
    ///     Writes a run of zero bytes, which the terminator and the room a declared buffer leaves over
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
    /// <param name="Size">The size of the string buffer, in bytes</param>
    private readonly record struct Buffer(int[] Lengths, uint[] Offsets, long Size)
    {
        /// <summary>
        ///     Gets the size the buffer occupies once no declared room is left over
        /// </summary>
        /// <param name="unit">The number of bytes one character takes in the container</param>
        /// <returns>The size of the entries and their terminators, in bytes</returns>
        public long Used(int unit)
        {
            return Offsets.Length == 0 ? 0 : (Offsets[^1] + (uint)Lengths[^1] + 1) * (uint)unit;
        }

        /// <summary>
        ///     Gets the size of the buffer in the units the container counts it in
        /// </summary>
        /// <param name="unit">The number of bytes one character takes in the container</param>
        /// <returns>The size of the string buffer, in units</returns>
        public uint Units(int unit)
        {
            return (uint)(Size / unit);
        }
    }
}