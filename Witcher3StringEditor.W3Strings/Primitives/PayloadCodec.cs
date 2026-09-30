using System.Text;

namespace Witcher3StringEditor.W3Strings.Primitives;

internal static class PayloadCodec
{
    private static ushort Rotl16(ushort x)
    {
        return (ushort)(((x << 1) | (x >> 15)) & 0xFFFF);
    }

    private static void XorUtf8(Span<byte> data, int length, uint magic)
    {
        var key = (ushort)((magic >> 8) & 0xFFFF);
        for (var i = 0; i < length; i++)
        {
            var charKey = (byte)((uint)((length + 1) * key) & 0xFF);
            data[i] ^= charKey;
            key = Rotl16(key);
        }
    }

    private static void XorUtf16(Span<byte> data, int length, uint magic)
    {
        var key = (ushort)((magic >> 8) & 0xFFFF);
        for (var i = 0; i < length; i++)
        {
            var charKey = (ushort)((uint)((length + 1) * key) & 0xFFFF);
            data[i * 2] ^= (byte)(charKey & 0xFF);
            data[i * 2 + 1] ^= (byte)(charKey >> 8);
            key = Rotl16(key);
        }
    }


    public static string Decode(Span<byte> stored, int length, uint magic, int unit)
    {
        if (unit == 2) XorUtf16(stored, length, magic);
        else XorUtf8(stored, length, magic);

        return unit == 2
            ? Encoding.Unicode.GetString(stored)
            : Encoding.UTF8.GetString(stored);
    }

    /// <summary>
    ///     Measures how many units a text occupies once encoded, without producing the bytes
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="unit">The number of bytes one character takes in the container</param>
    /// <returns>The length of the payload, in units</returns>
    /// <remarks>
    ///     The result is the length <see cref="Encode" /> reports: an encoding produces the same number
    ///     of bytes however they are asked for
    /// </remarks>
    public static int Measure(string text, int unit)
    {
        var bytes = unit == 2 ? Encoding.Unicode.GetByteCount(text) : Encoding.UTF8.GetByteCount(text);
        return bytes / unit;
    }

    public static byte[] Encode(string text, uint magic, int unit, out int length)
    {
        var raw = unit == 2 ? Encoding.Unicode.GetBytes(text) : Encoding.UTF8.GetBytes(text);
        length = raw.Length / unit;
        if (unit == 2) XorUtf16(raw, length, magic);
        else XorUtf8(raw, length, magic);
        return raw;
    }
}