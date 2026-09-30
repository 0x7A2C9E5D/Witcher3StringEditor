namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringsFile
{
    public uint Version { get; init; } = W3StringsFormat.FirstUtf8Version;

    /// <summary>
    ///     The full 32-bit language key the container was written with, which is what identifies the
    ///     language. The container stores it in two halves: the head one in its header, the tail one in
    ///     its very last two bytes
    /// </summary>
    public uint Key { get; init; }

    /// <summary>
    ///     The head half of the language key, as it is stored in the header
    /// </summary>
    public ushort Key1 => (ushort)(Key >> 16);

    /// <summary>
    ///     The tail half of the language key, as it is stored in the last two bytes of the container
    /// </summary>
    public ushort Key2 => (ushort)(Key & 0xFFFF);

    public W3Language? Language { get; init; }

    public uint Magic { get; init; }

    public int Unit => W3StringsFormat.OffsetUnitSize(Version);

    public List<W3StringEntry> Strings { get; } = [];

    public List<W3KeyEntry> Keys { get; } = [];

    public uint DeclaredBufferUnits { get; init; }

    public byte[] Trailer { get; init; } = [];
}