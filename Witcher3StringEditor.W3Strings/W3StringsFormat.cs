namespace Witcher3StringEditor.W3Strings;

/// <summary>
///     The fixed facts of the w3strings container layout: its magic, the size of its sections and the
///     versions it is written in
/// </summary>
public static class W3StringsFormat
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
    ///     The size of the offset unit in bytes: 1 byte for UTF-8, 2 bytes for UTF-16LE
    /// </summary>
    /// <param name="version">The version of the container</param>
    /// <returns>The size of the offset unit in bytes</returns>
    public static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }
}
