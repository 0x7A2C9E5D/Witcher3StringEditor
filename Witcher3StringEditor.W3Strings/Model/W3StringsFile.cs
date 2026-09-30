namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringsFile
{
    /// <summary>
    ///     The version the container is written with, which decides how its texts are encoded
    /// </summary>
    /// <remarks>
    ///     There is no default: the version is what a caller knows about the container it is building,
    ///     and a container written with a version nobody chose is a container nobody meant to write
    /// </remarks>
    public required uint Version { get; init; }

    /// <summary>
    ///     The full 32-bit language key the container is written with, which is what identifies the
    ///     language. The container stores it in two halves: the head one in its header, the tail one in
    ///     its very last two bytes
    /// </summary>
    /// <remarks>
    ///     There is no default: a container written with a key nobody chose is a container nobody meant
    ///     to write, and the key is what every id and text of it is obfuscated according to
    /// </remarks>
    public required uint Key { get; init; }

    /// <summary>
    ///     The head half of the language key, as it is stored in the header
    /// </summary>
    public ushort Key1 => (ushort)(Key >> 16);

    /// <summary>
    ///     The tail half of the language key, as it is stored in the last two bytes of the container
    /// </summary>
    public ushort Key2 => (ushort)(Key & 0xFFFF);

    /// <summary>
    ///     The magic every id and text of the container is obfuscated with
    /// </summary>
    /// <remarks>
    ///     It follows from the key and is never set on its own: the codec decodes and encodes with the
    ///     magic, and the language is only how the key is turned into one
    /// </remarks>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows</exception>
    public uint Magic => W3StringsFormat.MagicOf(Key);

    public int Unit => W3StringsFormat.OffsetUnitSize(Version);

    public List<W3StringEntry> Strings { get; } = [];

    public List<W3KeyEntry> Keys { get; } = [];
}