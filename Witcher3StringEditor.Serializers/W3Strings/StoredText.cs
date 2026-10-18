using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The stored form of one text: the bytes a container holds for it, and the number of characters
///     those bytes take.
/// </summary>
/// <remarks>
///     Writing runs <c>text --From--> stored text --Bytes--> container</c>, reading runs
///     <c>stored bytes --FromBytes--> stored text --Decode--> text</c>. The text is encoded as UTF-8 or
///     UTF-16LE (one or two bytes per character) and then obfuscated with a key rotated one bit per
///     character. The key follows from the character count, so <see cref="Length" /> is kept with the
///     bytes.
/// </remarks>
internal sealed class StoredText
{
    /// <summary>
    ///     The number of bytes one character takes in the UTF-8 generation.
    /// </summary>
    private const int Utf8UnitSize = 1;

    /// <summary>
    ///     The number of bytes one character takes in the UTF-16LE generation.
    /// </summary>
    private const int Utf16UnitSize = 2;

    /// <summary>
    ///     Holds the bytes of a text and the length they were stored with.
    /// </summary>
    /// <param name="bytes">The bytes the text is stored as, already obfuscated.</param>
    /// <param name="length">The length of the text, in characters.</param>
    private StoredText(byte[] bytes, int length)
    {
        Bytes = bytes;
        Length = length;
    }

    /// <summary>
    ///     The bytes the text is stored as: its encoding, obfuscated with the magic of the container.
    /// </summary>
    /// <remarks>
    ///     Written out as they are. <see cref="Decode" /> works on a copy and leaves these usable.
    /// </remarks>
    public byte[] Bytes { get; }

    /// <summary>
    ///     The length of the text, in the units the container counts in: one byte (UTF-8) or two (UTF-16LE)
    ///     per character.
    /// </summary>
    /// <remarks>
    ///     The number of characters the bytes hold rather than their size, which is <see cref="Bytes" />.
    ///     Kept here because the obfuscating key follows from it.
    /// </remarks>
    private int Length { get; }

    /// <summary>
    ///     Stores a text: turns it into the bytes a container holds for it.
    /// </summary>
    /// <param name="text">The text to store.</param>
    /// <param name="magic">The magic of the container the text goes into.</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" />.</param>
    /// <returns>The stored text, ready to be written into a container.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two.</exception>
    public static StoredText From(string text, uint magic, int unit)
    {
        var bytes = EncodeToBytes(text, unit);
        var length = bytes.Length / unit;
        return new StoredText(Obfuscate(bytes, length, magic, unit), length);
    }

    /// <summary>
    ///     Takes the bytes a container holds for a text, as they are stored.
    /// </summary>
    /// <param name="stored">The bytes the text is stored as, taken as they are.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" />.</param>
    /// <returns>The stored text, ready for <see cref="Decode" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="stored" /> does not hold exactly
    ///     <paramref name="length" /> characters.
    /// </exception>
    /// <remarks>
    ///     The bytes are not copied or obfuscated again: a reader hands over the stored bytes it read out of
    ///     the string buffer.
    /// </remarks>
    public static StoredText FromBytes(byte[] stored, int length, int unit)
    {
        CheckLength(stored.Length, length, unit);
        return new StoredText(stored, length);
    }

    /// <summary>
    ///     Reads the text out of the bytes it is stored as.
    /// </summary>
    /// <param name="magic">The magic of the container holding the text.</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" />.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or the stored bytes do not hold exactly
    ///     <see cref="Length" /> characters.
    /// </exception>
    /// <remarks>
    ///     Deobfuscating changes the bytes in place, so this works on a copy and leaves <see cref="Bytes" />
    ///     usable.
    /// </remarks>
    public string Decode(uint magic, int unit)
    {
        return DecodeFromBytes(Deobfuscate([.. Bytes], Length, magic, unit), Length, unit);
    }

    /// <summary>
    ///     Counts the characters a text takes, in the units a container measures them in.
    /// </summary>
    /// <param name="text">The text to count.</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE.</param>
    /// <returns>The length of the text, in characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two.</exception>
    /// <remarks>
    ///     Counts the <see cref="Length" /> a stored text would hold without producing any bytes. A writer
    ///     measures every text this way before laying the string buffer out, since the offsets and lengths
    ///     block sits before it.
    /// </remarks>
    public static int EncodedLength(string text, int unit)
    {
        CheckUnit(unit);
        var bytes = unit == Utf16UnitSize ? Encoding.Unicode.GetByteCount(text) : Encoding.UTF8.GetByteCount(text);
        return bytes / unit;
    }

    /// <summary>
    ///     Deobfuscates the bytes of a text in place, turning stored bytes into the bytes of the text.
    /// </summary>
    /// <param name="stored">The stored bytes, which come out deobfuscated.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="magic">The magic of the container holding the text.</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" />.</param>
    /// <returns>The same bytes, deobfuscated, ready for <see cref="DecodeFromBytes" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="stored" /> does not hold exactly
    ///     <paramref name="length" /> characters.
    /// </exception>
    private static Span<byte> Deobfuscate(Span<byte> stored, int length, uint magic, int unit)
    {
        CheckLength(stored.Length, length, unit);
        return ObfuscateCharacters(stored, length, unit, FirstKey(magic));
    }

    /// <summary>
    ///     Obfuscates the bytes of a text in place, turning the bytes of a text into stored bytes.
    /// </summary>
    /// <param name="bytes">The bytes of the text, which come out obfuscated.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="magic">The magic of the container the text goes into.</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" />.</param>
    /// <returns>The same bytes, obfuscated, ready to be written into a container.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="bytes" /> does not hold exactly
    ///     <paramref name="length" /> characters.
    /// </exception>
    /// <remarks>
    ///     Obfuscating twice gives the bytes back as they were, so this and <see cref="Deobfuscate" /> do
    ///     the same thing under different names.
    /// </remarks>
    private static byte[] Obfuscate(byte[] bytes, int length, uint magic, int unit)
    {
        CheckLength(bytes.Length, length, unit);
        ObfuscateCharacters(bytes, length, unit, FirstKey(magic));
        return bytes;
    }

    /// <summary>
    ///     Writes a text as the bytes of its encoding.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE.</param>
    /// <returns>The bytes of the text, not yet obfuscated.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two.</exception>
    private static byte[] EncodeToBytes(string text, int unit)
    {
        CheckUnit(unit);
        return unit == Utf16UnitSize ? Encoding.Unicode.GetBytes(text) : Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    ///     Reads a text out of the bytes of its encoding.
    /// </summary>
    /// <param name="bytes">The bytes of the text, already deobfuscated.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="bytes" /> does not hold exactly
    ///     <paramref name="length" /> characters.
    /// </exception>
    private static string DecodeFromBytes(ReadOnlySpan<byte> bytes, int length, int unit)
    {
        CheckLength(bytes.Length, length, unit);
        return unit == Utf16UnitSize
            ? Encoding.Unicode.GetString(bytes)
            : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///     The key the first character of a text is obfuscated with.
    /// </summary>
    /// <param name="magic">The magic of the container.</param>
    /// <returns>The key of the first character.</returns>
    /// <remarks>
    ///     Bits 8 to 23 of the magic, not its upper half: for 0x79321793 the key is 0x3217, while its upper
    ///     half 0x7932 turns the texts into nonsense.
    /// </remarks>
    private static ushort FirstKey(uint magic)
    {
        return (ushort)((magic >> 8) & 0xFFFF);
    }

    /// <summary>
    ///     Obfuscates every character of a text, one rotation of the key per character.
    /// </summary>
    /// <param name="bytes">The bytes of the text, which come out obfuscated.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="unit">The number of bytes one character takes.</param>
    /// <param name="key">The key the first character is obfuscated with.</param>
    /// <returns>The same bytes, obfuscated.</returns>
    private static Span<byte> ObfuscateCharacters(Span<byte> bytes, int length, int unit, ushort key)
    {
        for (var i = 0; i < length; i++)
        {
            // The character count is part of every character's key, so a text cannot be decoded without it.
            var characterKey = (length + 1) * (uint)key;
            if (unit == Utf16UnitSize)
            {
                bytes[i * 2] ^= (byte)characterKey;
                bytes[i * 2 + 1] ^= (byte)(characterKey >> 8);
            }
            else
            {
                bytes[i] ^= (byte)characterKey;
            }

            key = Rotate(key);
        }

        return bytes;
    }

    /// <summary>
    ///     Rotates the key one bit to the left, which is how it moves from one character to the next.
    /// </summary>
    /// <param name="key">The key of a character.</param>
    /// <returns>The key of the character after it.</returns>
    private static ushort Rotate(ushort key)
    {
        return (ushort)(((key << 1) | (key >> 15)) & 0xFFFF);
    }

    /// <summary>
    ///     Checks that a unit is one of the two the format has.
    /// </summary>
    /// <param name="unit">The number of bytes one character takes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two.</exception>
    private static void CheckUnit(int unit)
    {
        if (unit is not (Utf8UnitSize or Utf16UnitSize))
            throw new ArgumentOutOfRangeException(nameof(unit), unit,
                "one character takes either one byte (UTF-8) or two (UTF-16LE)");
    }

    /// <summary>
    ///     Checks that a buffer holds exactly the characters a text is said to have.
    /// </summary>
    /// <param name="bytes">The size of the buffer, in bytes.</param>
    /// <param name="length">The length of the text, in characters.</param>
    /// <param name="unit">The number of bytes one character takes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or the buffer and the length disagree.
    /// </exception>
    /// <remarks>
    ///     Both numbers come from the container, so they are compared instead of letting a text walk off the
    ///     end of its buffer.
    /// </remarks>
    private static void CheckLength(int bytes, int length, int unit)
    {
        CheckUnit(unit);
        if (length < 0 || (long)length * unit != bytes)
            throw new ArgumentOutOfRangeException(nameof(length), (long)length,
                $"a text that long takes {length * (long)unit} byte(s), but the buffer holds {bytes}");
    }
}