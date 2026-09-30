namespace Witcher3StringEditor.W3Strings;

internal static class W3StringsFormat
{
    public const int MinSize = 16;
    
    public const int FirstUtf8Version = 164;

    public const int Block1EntrySize = 12;

    public const int Block2EntrySize = 8;

    public const int Key1Offset = 8;

    public const int FirstCountOffset = 10;

    public static ReadOnlySpan<byte> MagicBytes => "RTSW"u8;

    public static int OffsetUnitSize(uint version)
    {
        return version >= FirstUtf8Version ? 1 : 2;
    }
}