namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The container of strings
/// </summary>
/// <remarks>
///     The container is what is written to disk, and is what is read from disk. It holds the texts and the
///     keys that resolve them and nothing else: the version it is written with is the choice of the caller
///     that writes it, because the same texts can be written for either generation of the game
/// </remarks>
/// <exception cref="W3StringsException">Thrown when the container is not one this build knows</exception>
internal sealed class W3StringsFile
{
    /// <summary>
    ///     The full 32-bit language key the container is written with, which is what identifies the
    ///     language. The container stores it in two halves: the head one in its header, the tail one in
    ///     Its very last two bytes
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

    /// <summary>
    ///     The strings of the container, in the order they are stored
    /// </summary>
    public List<W3StringEntry> Strings { get; } = [];

    /// <summary>
    ///     The keys of the container, in the order they are stored
    /// </summary>
    /// <remarks>
    ///     The keys are what resolve the ids of the strings to the strings themselves
    /// </remarks>
    public List<W3KeyEntry> Keys { get; } = [];
}