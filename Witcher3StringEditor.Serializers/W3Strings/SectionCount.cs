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
    ///     The byte that introduces a section holding nothing
    /// </summary>
    /// <remarks>
    ///     An empty section is spelled with the top bit of its only byte set rather than as a plain zero,
    ///     which is how every container that holds one spells it. The reader takes the six data bits of that
    ///     byte and nothing else, and those are zero either way, so both spellings read as the same count
    /// </remarks>
    private const byte Empty = 0x80;

    /// <summary>
    ///     The number of bytes a count takes at most: six data bits in the first byte and seven in each of
    ///     the four that can follow carry the 32 bits of a count with room to spare
    /// </summary>
    private const int MaxLength = 5;

    /// <summary>
    ///     Reads the count that introduces a section from a stream
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <returns>The count</returns>
    /// <exception cref="W3StringsException">Thrown when the stream ends inside the count, or holds no valid one</exception>
    /// <remarks>
    ///     The first byte carries six data bits and, in bit 6, whether another byte follows it; every byte
    ///     after it carries seven data bits and says the same in bit 7. The first byte whose flag is clear
    ///     therefore closes the count, which is what makes the first byte six bits wide and every later one
    ///     seven, and what lets an empty section be spelled <see cref="Empty" />
    /// </remarks>
    public static uint Read(Stream input)
    {
        var first = input.ReadByte();
        if (first < 0)
            throw new W3StringsException("bit6: the stream ends where a count has to stand");

        if ((first & 0x40) == 0) return (uint)(first & 0x3F); // The flag is clear: the count is this byte

        // Every byte after the first one holds seven bits, shifted past the bits the bytes before it held.
        var value = (ulong)(first & 0x3F);
        for (var length = 2; length <= MaxLength; length++)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new W3StringsException("bit6: the stream ends inside a count");

            value |= (ulong)(next & 0x7F) << (6 + 7 * (length - 2));

            // The flag of this byte is clear, so the count ends here.
            if ((next & 0x80) == 0)
                return value <= uint.MaxValue
                    ? (uint)value
                    : throw new W3StringsException(
                        $"bit6: the count {value} does not fit a 32-bit unsigned integer");
        }

        throw new W3StringsException(
            $"bit6: the count does not end inside the {MaxLength} bytes a count can take");
    }

    /// <summary>
    ///     Writes a count the way a container stores it
    /// </summary>
    /// <param name="value">The count to encode</param>
    /// <returns>The encoded count</returns>
    /// <remarks>
    ///     The first byte takes the six lowest bits and marks that another byte follows it, every byte
    ///     between takes seven bits and marks the same, and the last byte takes the bits that are left and
    ///     leaves its flag clear. A count that fits the six bits of the first byte is that byte alone, and
    ///     an empty section is <see cref="Empty" />
    /// </remarks>
    public static byte[] Write(uint value)
    {
        if (value == 0) return [Empty];

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
        return bytes;
    }

    /// <summary>
    ///     Counts the bytes a count is written in
    /// </summary>
    /// <param name="value">The count to measure</param>
    /// <returns>The number of bytes the count takes</returns>
    private static int LengthOf(uint value)
    {
        var length = 1;
        for (var remaining = value >> 6; remaining > 0; remaining >>= 7) length++;
        return length;
    }
}