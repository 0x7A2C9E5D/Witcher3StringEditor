namespace Witcher3StringEditor.W3Strings.Primitives;

public static class VariableLengthCodec
{
    public static (uint Value, int NextOffset) Read(ReadOnlySpan<byte> data, int offset)
    {
        ulong value = 0;
        var shift = 0;
        var index = 1;
        var p = offset;
        while (true)
        {
            if ((uint)p >= (uint)data.Length)
                throw new W3StringsException("bit6: unexpected end of data while reading a count");

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

            value |= (ulong)(x & mask) << shift;
            shift += step;

            if (x < 64 || (index >= 3 && x < 128)) break;
            index++;
        }

        return value > uint.MaxValue
            ? throw new W3StringsException($"bit6: value {value} out of range")
            : ((uint)value, p);
    }

    public static byte[] Write(uint value)
    {
        for (var groupCount = 1; groupCount <= 6; groupCount++)
            foreach (var terminatorBits in new[] { 6, 7, 8 })
            {
                var candidate = Build(value, groupCount, terminatorBits);
                if (candidate is null) continue;
                if (Read(candidate, 0).Value == value) return candidate;
            }

        throw new W3StringsException($"bit6: cannot encode {value}");
    }

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