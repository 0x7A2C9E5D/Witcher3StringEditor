namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The container of strings: the texts and the keys that resolve them.
/// </summary>
/// <remarks>
///     It holds neither the version nor the byte layout, which belong to the file and to
///     <see cref="W3StringsStorage" />.
/// </remarks>
/// <exception cref="W3StringsException">Thrown when the container is not one this build knows.</exception>
internal sealed class W3StringsFile
{
    /// <summary>
    ///     The full 32-bit language key the container is written with.
    /// </summary>
    public required uint Key { get; init; }

    /// <summary>
    ///     The magic the ids and texts of the container are obfuscated with, derived from the key.
    /// </summary>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows.</exception>
    public uint Magic => W3StringsStorage.MagicOf(Key);

    /// <summary>
    ///     The strings of the container, in the order they are stored.
    /// </summary>
    public List<W3StringEntry> Strings { get; } = [];

    /// <summary>
    ///     The keys of the container, in the order they are stored.
    /// </summary>
    public List<W3KeyEntry> Keys { get; } = [];
}