namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     The container of strings
/// </summary>
/// <remarks>
///     <para>
///         The container is what is written to disk, and is what is read from disk. It holds the texts and
///         the keys that resolve them, and no version: the version is a fact of the file rather than of the
///         container, because the same texts are written for either generation of the game and the encoding
///         they are stored in is the choice of whoever writes them. A writer is therefore told the version to
///         write, and a reader answers with the version it found.
///     </para>
///     <para>
///         The layout of the bytes is not a fact of the container either: how its strings are laid out in a
///         file, how long each of them is stored with and which id each of them is stored under are the
///         business of <see cref="W3StringsStorage" />, which is the stored form of a container and the only
///         place the format is written down. The entries it holds are the ones defined here, so a string
///         carries the slot the file gives it while the container is being read or written
///     </para>
/// </remarks>
/// <exception cref="W3StringsException">Thrown when the container is not one this build knows</exception>
internal sealed class W3StringsFile
{
    /// <summary>
    ///     The full 32-bit language key the container is written with, which is what identifies the
    ///     language
    /// </summary>
    /// <remarks>
    ///     There is no default: a container written with a key nobody chose is a container nobody meant
    ///     to write, and the key is what every id and text of it is obfuscated according to
    /// </remarks>
    public required uint Key { get; init; }

    /// <summary>
    ///     The magic the ids and texts of the container are obfuscated with
    /// </summary>
    /// <remarks>
    ///     It follows from the key and is never set on its own: the stored form decodes and encodes with the
    ///     magic, and the language is only how the key is turned into one
    /// </remarks>
    /// <exception cref="W3StringsException">Thrown when the language key is not one this build knows</exception>
    public uint Magic => W3StringsStorage.MagicOf(Key);

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