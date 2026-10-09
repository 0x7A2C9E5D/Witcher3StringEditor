using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The fixed facts of the w3strings container layout: its magic, the size of its sections and the
///     versions it is written in
/// </summary>
internal static class W3StringsFormat
{
    /// <summary>
    ///     The size a file has to reach before it can hold a container at all: the head (the magic, the
    ///     version and the head half of the language key), the count that introduces each of the three
    ///     sections, one byte at the least, and the tail half of the language key that closes it
    /// </summary>
    /// <remarks>
    ///     The smallest container, the one that holds no entry at all, is exactly this size: writing one has
    ///     to leave a file this reader can read back
    /// </remarks>
    public const int MinSize = 15;

    /// <summary>
    ///     The version of the UTF-8 generation: one byte per character, so every offset and length counts
    ///     bytes. Every version from this one on is read that way
    /// </summary>
    public const uint FirstUtf8Version = 164;

    /// <summary>
    ///     The version the classic UTF-16LE generation is written with: two bytes per character, so every
    ///     offset and length counts characters
    /// </summary>
    /// <remarks>
    ///     It is the version a save writes for that generation, and the one every version below the UTF-8
    ///     generation is read as: a container of the game declares 162 or 163, and both count their offsets,
    ///     lengths and buffer size in characters of two bytes
    /// </remarks>
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
    ///     other key that owns no language means the texts were obfuscated with a magic this build does
    ///     not know, and decoding it as if it were cleartext would turn every string into garbage
    ///     without a single error, which is worse than refusing the file
    /// </remarks>
    public static uint MagicOf(uint key)
    {
        var language = Language.FromKey(key);
        if (language is not null) return language.Magic;
        if (key == 0) return 0;
        throw new W3StringsException(
            $"unknown language key 0x{key:X8}, so the stored texts cannot be decoded " +
            "(the language is not supported by this build)");
    }

    /// <summary>
    ///     The size of the offset unit in bytes: 1 byte for a container of the UTF-8 generation, 2 bytes for
    ///     one of the UTF-16LE generation
    /// </summary>
    /// <param name="version">The version of the container</param>
    /// <returns>The size of the offset unit in bytes</returns>
    /// <remarks>
    ///     Everything below the UTF-8 generation is counted in characters of two bytes, so 162 and 163 are
    ///     answered for alike: the older generation of the game is one layout, and the version a container of
    ///     it declares is an upper bound rather than a difference. A reader asks it for the version it worked
    ///     out the container is really stored in, which is the one it declares unless it declares one
    ///     generation and holds the other: see W3StringsReader for those
    /// </remarks>
    public static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }

    /// <summary>
    ///     Gets the version a container asked to be written with a version is written as
    /// </summary>
    /// <param name="version">The version the container is asked to be written with</param>
    /// <returns>The version the container is written as</returns>
    /// <remarks>
    ///     The older generation of the game has one layout, which is the one version 162 describes, so every
    ///     version below the UTF-8 generation is written as 162: a file that declared another version of that
    ///     generation, 163 among them, would only make a reader guess at a difference that is not there. The
    ///     UTF-8 generation is written with the version it is asked for
    /// </remarks>
    public static uint WrittenVersion(uint version)
    {
        return version >= FirstUtf8Version ? version : Utf16LeVersion;
    }
}