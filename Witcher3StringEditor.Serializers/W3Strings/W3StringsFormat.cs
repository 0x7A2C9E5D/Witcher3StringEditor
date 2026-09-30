using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The fixed facts of the w3strings container layout: its magic, the size of its sections and the
///     versions it is written in
/// </summary>
internal static class W3StringsFormat
{
    /// <summary>
    ///     The size a file has to reach before it can hold the fixed head and tail of a container at all
    /// </summary>
    public const int MinSize = 16;

    /// <summary>
    ///     The version of the UTF-8 generation: one byte per character, so every offset and length
    ///     counts bytes
    /// </summary>
    public const uint FirstUtf8Version = 164;

    /// <summary>
    ///     The version the classic UTF-16LE generation is written with: two bytes per character, so
    ///     every offset and length counts characters. Every version below the UTF-8 generation is read
    ///     that way
    /// </summary>
    public const uint Utf16LeVersion = 162;

    /// <summary>
    ///     The size of the first block of entries: 12 bytes
    /// </summary>
    public const int Block1EntrySize = 12;

    /// <summary>
    ///     The size of the second block of entries: 8 bytes
    /// </summary>
    public const int Block2EntrySize = 8;

    /// <summary>
    ///     The offset of the first key in the container: 8 bytes
    /// </summary>
    public const int Key1Offset = 8;

    /// <summary>
    ///     The offset of the second key in the container: 10 bytes
    /// </summary>
    public const int FirstCountOffset = 10;

    /// <summary>
    ///     The magic bytes of the container: "RTSW"
    /// </summary>
    public static ReadOnlySpan<byte> MagicBytes => "RTSW"u8;

    /// <summary>
    ///     Gets the magic a container of the given language key obfuscates its ids and texts with
    /// </summary>
    /// <param name="key">The full 32-bit language key, both halves together</param>
    /// <returns>The magic of the language that owns the key, or zero for a key no language owns</returns>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows</exception>
    /// <remarks>
    ///     Every language the game added after its release shares key 0, so a zero key identifies no
    ///     language but is not an error either: those containers are stored without obfuscation. Any
    ///     other key that owns no language means the payload was obfuscated with a magic this build does
    ///     not know, and decoding it as if it were cleartext would turn every string into garbage
    ///     without a single error, which is worse than refusing the file
    /// </remarks>
    public static uint MagicOf(uint key)
    {
        var language = W3Language.FromKey(key);
        if (language is not null) return language.Magic;
        if (key == 0) return 0;
        throw new W3StringsException(
            $"unknown language key 0x{key:X8}, so the string payload cannot be decoded " +
            "(the language is not supported by this build)");
    }

    /// <summary>
    ///     The size of the offset unit in bytes: 1 byte for UTF-8, 2 bytes for UTF-16LE
    /// </summary>
    /// <param name="version">The version of the container</param>
    /// <returns>The size of the offset unit in bytes</returns>
    public static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }
}