using System.Text;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     Encodes a container into a stream
///     The block of offsets and lengths sits before the string buffer, so the whole layout has to be
///     known before the first byte is written. The lengths are therefore measured in a first pass, which
///     only needs the size of every text and never the encoded bytes themselves
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
        var lengths = MeasureEntries(file);
        var offsets = ResolveOffsets(file, lengths);
        WriteContainer(output, file, lengths, offsets);
    }

    /// <summary>
    ///     Measures how many units every entry occupies, encoding nothing
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <returns>The length of every entry, in units</returns>
    private static int[] MeasureEntries(W3StringsFile file)
    {
        var unit = file.Unit;
        var lengths = new int[file.Strings.Count];
        for (var i = 0; i < file.Strings.Count; i++)
            lengths[i] = CanReusePlainBytes(file.Strings[i], unit, out var storedLength)
                ? storedLength
                : PayloadCodec.Measure(file.Strings[i].Value, unit);

        return lengths;
    }

    /// <summary>
    ///     Lays the entries out in the string buffer
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <param name="lengths">The measured length of every entry</param>
    /// <returns>The offset of every entry, in units</returns>
    private static uint[] ResolveOffsets(W3StringsFile file, int[] lengths)
    {
        var offsets = new uint[file.Strings.Count];
        if (KeepsParsedLayout(file, lengths, offsets)) return offsets;

        uint cursor = 0;
        for (var i = 0; i < file.Strings.Count; i++)
        {
            offsets[i] = cursor;
            cursor += (uint)lengths[i] + 1;
        }

        return offsets;
    }

    /// <summary>
    ///     Keeps the layout a parsed container was read with, so that writing it back reproduces the
    ///     file byte for byte
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <param name="lengths">The measured length of every entry</param>
    /// <param name="offsets">Receives the parsed offsets while they are kept</param>
    /// <returns>True when the parsed layout still holds every entry</returns>
    /// <remarks>
    ///     An entry whose text was edited can grow past the room its original slot left it. The container
    ///     is then laid out afresh: keeping the old offsets would make two texts share the same bytes,
    ///     which is a corrupt container however it is written
    /// </remarks>
    private static bool KeepsParsedLayout(W3StringsFile file, int[] lengths, uint[] offsets)
    {
        if (file.Strings.Count == 0 || !file.Strings.All(entry => entry.StoredBytes is not null)) return false;

        for (var i = 0; i < file.Strings.Count; i++) offsets[i] = file.Strings[i].Offset;

        var order = Enumerable.Range(0, file.Strings.Count).OrderBy(index => offsets[index]).ToArray();
        for (var i = 1; i < order.Length; i++)
        {
            var previous = order[i - 1];
            var current = order[i];
            // Every entry needs its own length plus the terminator before the next one may start.
            if (offsets[current] >= offsets[previous] + lengths[previous] + 1) continue;
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Writes every section of the container in the order the format prescribes
    /// </summary>
    /// <param name="output">The stream the container is written to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="lengths">The measured length of every entry</param>
    /// <param name="offsets">The offset of every entry</param>
    private static void WriteContainer(Stream output, W3StringsFile file, int[] lengths, uint[] offsets)
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
            writer.Write(offsets[i]);
            writer.Write((uint)lengths[i]);
        }

        WriteCount(writer, (uint)file.Keys.Count);
        foreach (var key in file.Keys)
        {
            writer.Write(key.KeyHash);
            writer.Write(key.Id ^ magic);
        }

        var bufferSize = ResolveBufferSize(file, lengths, offsets);
        WriteCount(writer, (uint)(bufferSize / file.Unit));
        WriteBuffer(writer, file, lengths, offsets, bufferSize);

        if (file.Trailer.Length > 0) writer.Write(file.Trailer);
        writer.Write(file.Key2);
    }

    /// <summary>
    ///     Gets how many bytes the string buffer occupies: one terminator behind every entry, or the size
    ///     the container declared when that is larger
    /// </summary>
    /// <param name="file">The container to encode</param>
    /// <param name="lengths">The measured length of every entry</param>
    /// <param name="offsets">The offset of every entry</param>
    /// <returns>The size of the string buffer, in bytes</returns>
    private static long ResolveBufferSize(W3StringsFile file, int[] lengths, uint[] offsets)
    {
        var unit = file.Unit;
        long bufferEnd = 0;
        for (var i = 0; i < file.Strings.Count; i++)
            bufferEnd = Math.Max(bufferEnd, (offsets[i] + lengths[i] + 1) * unit);

        return Math.Max(bufferEnd, file.DeclaredBufferUnits * unit);
    }

    /// <summary>
    ///     Writes the string buffer, filling the gaps of an original layout with zeroes
    /// </summary>
    /// <param name="writer">The writer the buffer goes to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="lengths">The measured length of every entry</param>
    /// <param name="offsets">The offset of every entry</param>
    /// <param name="bufferSize">The size of the string buffer, in bytes</param>
    private static void WriteBuffer(BinaryWriter writer, W3StringsFile file, int[] lengths, uint[] offsets,
        long bufferSize)
    {
        var unit = file.Unit;
        var magic = file.Magic;
        long cursor = 0;

        // Entries are emitted in the order they occupy the buffer, not in the order they are listed.
        foreach (var i in Enumerable.Range(0, file.Strings.Count).OrderBy(index => offsets[index]))
        {
            var at = offsets[i] * unit;
            // An entry that starts inside the range of an earlier one has nowhere of its own to go.
            if (at < cursor) continue;

            WriteZeros(writer, at - cursor);
            var payload = EncodeEntry(file.Strings[i], magic, unit, lengths[i]);
            writer.Write(payload);
            WriteZeros(writer, unit); // The terminator that closes the text
            cursor = at + payload.Length + unit;
        }

        WriteZeros(writer, bufferSize - cursor); // Whatever the declared size leaves over
    }

    /// <summary>
    ///     Encodes one entry, reusing the bytes it was read with whenever they still describe it
    /// </summary>
    /// <param name="entry">The entry to encode</param>
    /// <param name="magic">The magic the payload is obfuscated with</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <param name="length">The measured length of the entry</param>
    /// <returns>The stored bytes of the entry</returns>
    private static byte[] EncodeEntry(W3StringEntry entry, uint magic, int unit, int length)
    {
        if (!CanReusePlainBytes(entry, unit, out var storedLength) || storedLength != length)
            return PayloadCodec.Encode(entry.Value, magic, unit, out _);
        // Obfuscate a copy, so the bytes the entry was read with stay available.
        var stored = (byte[])entry.PlainBytes!.Clone();
        if (unit == 2) PayloadCodec.XorUtf16(stored, length, magic);
        else PayloadCodec.XorUtf8(stored, length, magic);
        return stored;

        // The encoded length is the measured one, so it needs no second look.
    }

    /// <summary>
    ///     Decides whether an entry can be written from the bytes it was read with, which keeps a round
    ///     trip of an untouched container byte for byte
    /// </summary>
    /// <param name="entry">The entry to inspect</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <param name="length">Receives the length of the stored bytes, in units</param>
    /// <returns>True when the stored bytes still describe the entry</returns>
    private static bool CanReusePlainBytes(W3StringEntry entry, int unit, out int length)
    {
        length = 0;
        // An entry whose text was edited has to be encoded again.
        if (entry.OriginalValue is not null && entry.Value != entry.OriginalValue) return false;
        if (entry.PlainBytes is null || entry.PlainBytes.Length % unit != 0) return false;

        length = entry.PlainBytes.Length / unit;
        // The length the container declared has to agree with the bytes that are there.
        return entry.Length == 0 || entry.Length == length;
    }

    /// <summary>
    ///     Writes a run of zero bytes, which the terminator and the gaps of a layout are made of
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
}