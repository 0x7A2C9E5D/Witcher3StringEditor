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
    /// <param name="unit">The number of bytes one character takes in the version being written</param>
    /// <returns>The size the entries and their terminators occupy, and the size of the buffer in bytes</returns>
    /// <remarks>
    ///     The texts are laid out one behind the other in the order they are listed, each closed by the
    ///     terminator the format asks for, so the slot is recorded as the buffer is laid out and every entry
    ///     carries the offsets that point at its own text: the order of the block of offsets is then free,
    ///     and the buffer holds exactly what the texts need
    /// </remarks>
    private static (uint Used, long Size) BufferOf(W3StringsFile file, int unit)
    {
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
    /// <param name="version">The version to write it with, which the caller chose for the game it is for</param>
    /// <remarks>
    ///     The version is a parameter rather than a fact of the container, because the same texts are written
    ///     for either generation of the game: it decides how they are encoded and how their offsets, lengths
    ///     and buffer size are counted. A version below the UTF-8 generation is written as 162, the version
    ///     that generation is described by, so a container is never written with a version this build only
    ///     reads by guessing
    /// </remarks>
    public static void Write(Stream output, W3StringsFile file, uint version)
    {
        version = W3StringsFormat.WrittenVersion(version); // The older generation is always written as 162
        var unit = W3StringsFormat.OffsetUnitSize(version); // The bytes one character takes in that version
        // The layout has to be known before the first byte is written, because the block of offsets and
        // lengths sits before the string buffer it points into.
        var buffer = BufferOf(file, unit);
        var magic = file.Magic;
        using var writer = new BinaryWriter(output, Encoding.UTF8, true); // The stream stays open for the caller

        writer.Write(W3StringsFormat.MagicBytes);
        writer.Write(version);
        writer.Write(file.Key1);

        // The game resolves both blocks by binary search, and it searches them in the form they are stored
        // in: the second block by localization-key hash, which is stored as it is, and the first by string
        // id, which is stored obfuscated. Each block is therefore written in ascending order of the value
        // it holds. Ordering the entries by the clear ids is not the same order, because the obfuscating
        // xor does not keep the order of the values it is applied to: every language that has a magic then
        // stops resolving, while the containers of the languages without one keep working, and the game
        // shows the raw "##key" it asked for instead of the text. A block in the wrong order still loads
        // without an error.
        WriteCount(writer, (uint)file.Strings.Count);
        foreach (var entry in file.Strings.OrderBy(entry => entry.Id ^ magic))
        {
            writer.Write(entry.Id ^ magic);
            writer.Write(entry.Offset);
            writer.Write(entry.Length);
        }

        // A key entry whose hash is zero is not written: zero is how the format says "no key", so such an
        // entry is one no key can ever resolve to, and a container that holds one holds an entry the game
        // can do nothing with.
        var keys = file.Keys.Where(key => key.KeyHash != 0).OrderBy(key => key.KeyHash).ToList();
        WriteCount(writer, (uint)keys.Count);
        foreach (var key in keys)
        {
            writer.Write(key.KeyHash);
            writer.Write(key.Id ^ magic);
        }

        WriteCount(writer, (uint)(buffer.Size / unit));
        WriteBuffer(writer, file, buffer, unit);

        writer.Write(file.Key2);
    }

    /// <summary>
    ///     Writes the string buffer: every text, closed by its terminator, and the zeroes the size the
    ///     container gives the buffer leaves over
    /// </summary>
    /// <param name="writer">The writer the buffer goes to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="buffer">The sizes the buffer is written with</param>
    /// <param name="unit">The number of bytes one character takes in the version being written</param>
    private static void WriteBuffer(BinaryWriter writer, W3StringsFile file, (uint Used, long Size) buffer, int unit)
    {
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

    /// <summary>
    ///     Writes a count of the given value to the given writer
    /// </summary>
    /// <param name="writer">The writer the count goes to</param>
    /// <param name="value">The value to write as a count</param>
    private static void WriteCount(BinaryWriter writer, uint value)
    {
        writer.Write(SectionCount.Write(value));
    }
}