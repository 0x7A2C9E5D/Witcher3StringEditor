using System.Text;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The bytes a container stores for one text, and the text itself
/// </summary>
/// <remarks>
///     <para>
///         Storing a text is two steps, and reading one is the same two steps backwards. Both steps are
///         here, named after what they do, so the whole way from a text to the bytes of a container can be
///         read off this class:
///     </para>
///     <para>
///         writing: <c>text --EncodeToBytes--> bytes --Obfuscate--> stored bytes</c><br />
///         reading: <c>stored bytes --Deobfuscate--> bytes --DecodeFromBytes--> text</c>
///     </para>
///     <para>
///         <see cref="Encode" /> and <see cref="Decode" /> walk the whole way for a caller that wants the
///         result and nothing else; the single steps are public for a caller that wants to sit between
///         them, which is what looking for a text buffer does.
///     </para>
///     <para>
///         Step one writes the text as UTF-8 or UTF-16LE, so the number of bytes one character takes is
///         either one or two. Step two obfuscates every character with a key that rotates one bit per character,
///         and that key follows from the number of characters in the text: a text cannot be read without
///         knowing its length, which is why every method here takes it and checks the buffer against it.
///     </para>
/// </remarks>
internal static class StoredText
{
    /// <summary>
    ///     The number of bytes one character takes in the UTF-8 generation
    /// </summary>
    private const int Utf8UnitSize = 1;

    /// <summary>
    ///     The number of bytes one character takes in the UTF-16LE generation
    /// </summary>
    private const int Utf16UnitSize = 2;

    /// <summary>
    ///     Reads the text out of the bytes a container stores for it
    /// </summary>
    /// <param name="stored">The bytes the text is stored as, which are left as they were</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="magic">The magic of the container holding the text</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" /></param>
    /// <returns>The text</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="stored" /> does not hold exactly
    ///     <paramref name="length" /> characters
    /// </exception>
    /// <remarks>
    ///     Deobfuscating changes the bytes in place, so this hands <see cref="Deobfuscate" /> a copy: the stored
    ///     bytes stay usable. Call <see cref="Deobfuscate" /> followed by <see cref="DecodeFromBytes" /> instead
    ///     when the bytes are not needed afterward, or when something has to happen between the two steps
    /// </remarks>
    public static string Decode(ReadOnlySpan<byte> stored, int length, uint magic, int unit)
    {
        return DecodeFromBytes(Deobfuscate([.. stored], length, magic, unit), length, unit);
    }

    /// <summary>
    ///     Writes the bytes a container stores a text as
    /// </summary>
    /// <param name="text">The text to store</param>
    /// <param name="magic">The magic of the container the text goes into</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" /></param>
    /// <param name="length">Receives the length of the text, in characters</param>
    /// <returns>The bytes to store</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two</exception>
    /// <remarks>
    ///     The stored bytes are as long as the text is, so <paramref name="length" /> is the size of the
    ///     result divided by the unit: it is answered here so the caller does not have to measure the text
    ///     a second time with <see cref="EncodedLength" />
    /// </remarks>
    public static byte[] Encode(string text, uint magic, int unit, out int length)
    {
        var bytes = EncodeToBytes(text, unit);
        length = bytes.Length / unit;
        return Obfuscate(bytes, length, magic, unit);
    }

    /// <summary>
    ///     Counts the characters a text takes, in the units a container measures them in
    /// </summary>
    /// <param name="text">The text to count</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE</param>
    /// <returns>The length of the text, in characters</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two</exception>
    /// <remarks>
    ///     This is the length <see cref="Encode" /> answers, but counted without producing any bytes: a text
    ///     takes the same number of characters however it is asked for
    /// </remarks>
    public static int EncodedLength(string text, int unit)
    {
        CheckUnit(unit);
        var bytes = unit == Utf16UnitSize ? Encoding.Unicode.GetByteCount(text) : Encoding.UTF8.GetByteCount(text);
        return bytes / unit;
    }

    /// <summary>
    ///     Deobfuscates the bytes of a text in place, turning stored bytes into the bytes of the text
    /// </summary>
    /// <param name="stored">The stored bytes, which come out deobfuscated</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="magic">The magic of the container holding the text</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" /></param>
    /// <returns>The same bytes, deobfuscated, ready for <see cref="DecodeFromBytes" /></returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="stored" /> does not hold exactly
    ///     <paramref name="length" /> characters
    /// </exception>
    private static Span<byte> Deobfuscate(Span<byte> stored, int length, uint magic, int unit)
    {
        CheckLength(stored.Length, length, unit);
        return ObfuscateCharacters(stored, length, unit, FirstKey(magic));
    }

    /// <summary>
    ///     Obfuscates the bytes of a text in place, turning the bytes of a text into stored bytes
    /// </summary>
    /// <param name="bytes">The bytes of the text, which come out obfuscated</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="magic">The magic of the container the text goes into</param>
    /// <param name="unit">The number of bytes one character takes: see <see cref="EncodedLength" /></param>
    /// <returns>The same bytes, obfuscated, ready to be written into a container</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="bytes" /> does not hold exactly
    ///     <paramref name="length" /> characters
    /// </exception>
    /// <remarks>
    ///     Obfuscating a text twice gives the bytes back as they were, which is why one method serves both
    ///     directions: this and <see cref="Deobfuscate" /> do the same thing and are named for what the caller
    ///     is doing
    /// </remarks>
    private static byte[] Obfuscate(byte[] bytes, int length, uint magic, int unit)
    {
        CheckLength(bytes.Length, length, unit);
        ObfuscateCharacters(bytes, length, unit, FirstKey(magic));
        return bytes;
    }

    /// <summary>
    ///     Writes a text as the bytes of its encoding
    /// </summary>
    /// <param name="text">The text to write</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE</param>
    /// <returns>The bytes of the text, not yet obfuscated</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two</exception>
    private static byte[] EncodeToBytes(string text, int unit)
    {
        CheckUnit(unit);
        return unit == Utf16UnitSize ? Encoding.Unicode.GetBytes(text) : Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    ///     Reads a text out of the bytes of its encoding
    /// </summary>
    /// <param name="bytes">The bytes of the text, already deobfuscated</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="unit">The number of bytes one character takes: one for UTF-8, two for UTF-16LE</param>
    /// <returns>The text</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or <paramref name="bytes" /> does not hold exactly
    ///     <paramref name="length" /> characters
    /// </exception>
    private static string DecodeFromBytes(ReadOnlySpan<byte> bytes, int length, int unit)
    {
        CheckLength(bytes.Length, length, unit);
        return unit == Utf16UnitSize
            ? Encoding.Unicode.GetString(bytes)
            : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///     The key the first character of a text is obfuscated with
    /// </summary>
    /// <param name="magic">The magic of the container</param>
    /// <returns>The key of the first character</returns>
    /// <remarks>
    ///     The key is bits 8 to 23 of the magic rather than its upper half: the upper half of 0x79321793 is
    ///     0x7932, and reading a container of the game with that turns its texts into nonsense, while 0x3217
    ///     reads them
    /// </remarks>
    private static ushort FirstKey(uint magic)
    {
        return (ushort)((magic >> 8) & 0xFFFF);
    }

    /// <summary>
    ///     Obfuscates every character of a text, one rotation of the key per character
    /// </summary>
    /// <param name="bytes">The bytes of the text, which come out obfuscated</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="unit">The number of bytes one character takes</param>
    /// <param name="key">The key the first character is obfuscated with</param>
    /// <returns>The same bytes, obfuscated</returns>
    private static Span<byte> ObfuscateCharacters(Span<byte> bytes, int length, int unit, ushort key)
    {
        for (var i = 0; i < length; i++)
        {
            // The length of the text is part of the key of every one of its characters, which is why a
            // text cannot be read without knowing how many characters it has.
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
    ///     Rotates the key one bit to the left, which is how it moves from one character to the next
    /// </summary>
    /// <param name="key">The key of a character</param>
    /// <returns>The key of the character after it</returns>
    private static ushort Rotate(ushort key)
    {
        return (ushort)(((key << 1) | (key >> 15)) & 0xFFFF);
    }

    /// <summary>
    ///     Checks that a unit is one of the two the format has
    /// </summary>
    /// <param name="unit">The number of bytes one character takes</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit is neither one nor two</exception>
    private static void CheckUnit(int unit)
    {
        if (unit is not (Utf8UnitSize or Utf16UnitSize))
            throw new ArgumentOutOfRangeException(nameof(unit), unit,
                "one character takes either one byte (UTF-8) or two (UTF-16LE)");
    }

    /// <summary>
    ///     Checks that a buffer holds exactly the characters a text is said to have
    /// </summary>
    /// <param name="bytes">The size of the buffer, in bytes</param>
    /// <param name="length">The length of the text, in characters</param>
    /// <param name="unit">The number of bytes one character takes</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the unit is neither one nor two, or the buffer and the length disagree
    /// </exception>
    /// <remarks>
    ///     A container gives both numbers, so they are compared instead of letting a text walk off the end
    ///     of its buffer
    /// </remarks>
    private static void CheckLength(int bytes, int length, int unit)
    {
        CheckUnit(unit);
        if (length < 0 || (long)length * unit != bytes)
            throw new ArgumentOutOfRangeException(nameof(length), (long)length,
                $"a text that long takes {length * (long)unit} byte(s), but the buffer holds {bytes}");
    }
}