namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringsFile
{
    public uint Version { get; init; } = W3StringsFormat.FirstUtf8Version;

    public ushort Key1 { get; init; }

    public ushort Key2 { get; init; }

    public uint Key => ((uint)Key1 << 16) | Key2;

    public W3Language? Language { get; set; }

    public uint Magic { get; set; }

    public int Unit => W3StringsFormat.OffsetUnitSize(Version);

    public List<W3StringEntry> Strings { get; } = [];

    public List<W3KeyEntry> Keys { get; } = [];

    public uint DeclaredBufferUnits { get; set; }

    public byte[] Trailer { get; set; } = [];
}