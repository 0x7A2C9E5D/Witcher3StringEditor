namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The count that introduces each section of a w3strings container: how many entries or units follow
/// </summary>
/// <remarks>
///     Every section of a container is introduced by its size, written in the bit6 framing the format uses
///     for small numbers. A reader has to know that size before it can read the section, which is why the
///     three of them are read before anything else
/// </remarks>
internal static class SectionCount
{
    /// <summary>
    ///     Reads the count that introduces a section from a stream
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <returns>The count</returns>
    /// <exception cref="W3StringsException">Thrown when the stream ends inside the count, or holds no valid one</exception>
    public static uint Read(Stream input)
    {
        Span<byte> count = stackalloc byte[6]; // Six groups are the most a bit6 count can take
        for (var length = 1; length <= count.Length; length++)
        {
            var next = input.ReadByte();
            if (next < 0) break; // The stream ended inside the count
            count[length - 1] = (byte)next;

            // A count is complete as soon as the framing stops inside the bytes read so far.
            if (TryRead(count[..length], 0, out var value, out var end) && end == length) return value;
        }

        throw new W3StringsException(
            "bit6: the stream does not hold a readable count here, it ends inside one or it does not fit a 32-bit unsigned integer");
    }

    /// <summary>
    ///     Writes a count the way a container stores it
    /// </summary>
    /// <param name="value">The count to encode</param>
    /// <returns>The encoded count</returns>
    /// <exception cref="W3StringsException">Thrown when no framing holds the value</exception>
    public static byte[] Write(uint value)
    {
        for (var groupCount = 1; groupCount <= 6; groupCount++)
            foreach (var terminatorBits in new[] { 6, 7, 8 })
            {
                var candidate = Build(value, groupCount, terminatorBits);
                if (candidate is null) continue;
                // Probing has to be able to reject a framing, so it decodes without throwing: a
                // framing the reader cannot stop inside is merely the wrong one, not a broken file.
                // Probing with the throwing Read instead abandoned the search for counts such as
                // 8192, which a file can easily reach.
                if (TryRead(candidate, 0, out var decoded, out _) && decoded == value) return candidate;
            }

        // Six groups carry 6 + 7 * 4 + 8 = 42 bits, so every 32-bit value is encodable. Reaching this
        // line means the framing above is wrong, which is a bug and not a property of any input.
        throw new W3StringsException($"bit6: cannot encode {value}");
    }

    /// <summary>
    ///     Reads a bit6 encoded count without throwing
    /// </summary>
    /// <param name="data">The data to read from</param>
    /// <param name="offset">The offset the count starts at</param>
    /// <param name="value">Receives the count</param>
    /// <param name="nextOffset">Receives the offset that follows the count</param>
    /// <returns>True when a count was read</returns>
    private static bool TryRead(ReadOnlySpan<byte> data, int offset, out uint value, out int nextOffset)
    {
        value = 0;
        nextOffset = offset;

        ulong raw = 0;
        var shift = 0;
        var index = 1;
        var p = offset;
        while (true)
        {
            if ((uint)p >= (uint)data.Length) return false;

            var x = data[p++];
            uint mask;
            int step;

            switch (x)
            {
                case > 127:
                    mask = 0x7F;
                    step = 7;
                    break;
                case > 63 when index == 1:
                    mask = 0x3F;
                    step = 6;
                    break;
                default:
                    mask = 0xFF;
                    step = 6;
                    break;
            }

            raw |= (ulong)(x & mask) << shift;
            shift += step;

            if (x < 64 || (index >= 3 && x < 128)) break;
            index++;
        }

        if (raw > uint.MaxValue) return false;

        value = (uint)raw;
        nextOffset = p;
        return true;
    }

    /// <summary>
    ///     Frames one count out of the given number of groups
    /// </summary>
    /// <param name="value">The count to frame</param>
    /// <param name="groupCount">The number of bytes to use</param>
    /// <param name="terminatorBits">The number of data bits the last byte carries</param>
    /// <returns>The framed count, or null when this framing cannot hold the value</returns>
    private static byte[]? Build(uint value, int groupCount, int terminatorBits)
    {
        if (groupCount == 1)
            // A lone byte is read with the 8-bit mask and must stop immediately,
            return value < 64 ? [(byte)value] : null;

        // Bit budget: 6 in byte 0, 7 per interior byte, terminatorBits in the last.
        var totalBits = 6 + 7 * (groupCount - 2) + terminatorBits;
        if ((ulong)value >> totalBits != 0) return null;

        var bytes = new byte[groupCount];
        ulong v = value;

        bytes[0] = (byte)(v & 0x3F); // 6 data bits
        v >>= 6;
        for (var i = 1; i < groupCount - 1; i++) // 7 data bits each
        {
            bytes[i] = (byte)(v & 0x7F);
            v >>= 7;
        }

        bytes[groupCount - 1] = (byte)(v & ((1u << terminatorBits) - 1));
        v >>= terminatorBits;
        if (v != 0) return null;

        // Frame the groups so the decoder picks the intended masks.
        bytes[0] |= 0x40; // byte 0: continuation flag
        for (var i = 1; i < groupCount - 1; i++) // interior: 7-bit mask
            bytes[i] |= 0x80;

        // The terminator must stop the decoder.
        return (bytes[^1] & 0x40) != 0 ? null : bytes;
    }
}