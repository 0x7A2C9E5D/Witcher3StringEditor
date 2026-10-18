namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The size that introduces one section of a container, in the bit6 framing the format uses for small
///     numbers.
/// </summary>
internal sealed class SectionCount
{
    /// <summary>
    ///     The byte that introduces a section holding nothing.
    /// </summary>
    private const byte Empty = 0x80;

    /// <summary>
    ///     The number of bytes a count takes at most.
    /// </summary>
    private const int MaxLength = 5;

    /// <summary>
    ///     Holds the number a section is introduced by, and the bytes it is stored as.
    /// </summary>
    /// <param name="value">How many entries or units the section holds.</param>
    /// <param name="bytes">The bytes the count is stored as.</param>
    private SectionCount(uint value, byte[] bytes)
    {
        Value = value;
        Bytes = bytes;
    }

    /// <summary>
    ///     How many entries or units the section it introduces holds.
    /// </summary>
    public uint Value { get; }

    /// <summary>
    ///     The bytes the count is stored as, with the spelling it was read or computed with.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    ///     Reads the count that introduces a section from a stream.
    /// </summary>
    /// <param name="input">The stream to read from, left after the count.</param>
    /// <returns>The count, with the bytes it was stored as.</returns>
    /// <exception cref="W3StringsException">Thrown when the stream ends inside the count, or holds no valid one.</exception>
    public static SectionCount Read(Stream input)
    {
        var bytes = new byte[MaxLength]; // At most as many as a count can take
        var first = input.ReadByte();
        if (first < 0)
            throw new W3StringsException("bit6: the stream ends where a count has to stand");

        bytes[0] = (byte)first;
        var value = (ulong)(first & 0x3F); // The six lowest bits of the first byte
        if ((first & 0x40) == 0) return new SectionCount((uint)value, bytes[..1]); // The flag is clear

        // Every byte after the first holds seven bits, shifted past the bits the bytes before it held.
        for (var length = 2; length <= MaxLength; length++)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new W3StringsException("bit6: the stream ends inside a count");

            bytes[length - 1] = (byte)next;
            value |= (ulong)(next & 0x7F) << (6 + 7 * (length - 2));

            // The flag of this byte is clear, so the count ends here.
            if ((next & 0x80) != 0) continue;
            if (value > uint.MaxValue)
                throw new W3StringsException(
                    $"bit6: the count {value} does not fit a 32-bit unsigned integer");
            return new SectionCount((uint)value, bytes[..length]);
        }

        throw new W3StringsException(
            $"bit6: the count does not end inside the {MaxLength} bytes a count can take");
    }

    /// <summary>
    ///     Gets the count of a number, encoded the way a container stores it.
    /// </summary>
    /// <param name="value">The number of entries or units a section holds.</param>
    /// <returns>The count, with the bytes it is stored as.</returns>
    public static SectionCount Of(uint value)
    {
        if (value == 0) return new SectionCount(0, [Empty]);

        var bytes = new byte[LengthOf(value)];
        bytes[0] = (byte)(value & 0x3F); // The six lowest bits
        if (bytes.Length > 1) bytes[0] |= 0x40; // More bytes follow

        var remaining = value >> 6;
        for (var i = 1; i < bytes.Length - 1; i++)
        {
            bytes[i] = (byte)((remaining & 0x7F) | 0x80); // Seven bits, and more bytes follow
            remaining >>= 7;
        }

        if (bytes.Length > 1) bytes[^1] = (byte)remaining; // Seven bits, and the count ends here
        return new SectionCount(value, bytes);
    }

    /// <summary>
    ///     Counts the bytes a count is written in.
    /// </summary>
    /// <param name="value">The count to measure.</param>
    /// <returns>The number of bytes the count takes.</returns>
    private static int LengthOf(uint value)
    {
        var length = 1;
        for (var remaining = value >> 6; remaining > 0; remaining >>= 7) length++;
        return length;
    }
}