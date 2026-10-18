namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Writes a container into a stream
///     The stored form of the container is laid out by <see cref="W3StringsStorage" /> before the first byte
///     is written, because the block of offsets and lengths sits before the string buffer it points into;
///     this writes the bytes it lays down in the order the format holds them
/// </summary>
internal static class W3StringsWriter
{
    /// <summary>
    ///     Writes a container into the given stream, every section in the order the format prescribes
    /// </summary>
    /// <param name="output">The stream the container is written to</param>
    /// <param name="file">The container to encode</param>
    /// <param name="version">The version to write it with, which the caller chose for the game it is for</param>
    /// <remarks>
    ///     The version is a parameter rather than a fact of the container, because the same texts are written
    ///     for either generation of the game: it decides how their bytes are encoded and how their offsets,
    ///     lengths and buffer size are counted. A version below the UTF-8 generation is written as 162, the
    ///     version that generation is described by, so a container is never written with a version this build
    ///     only reads by guessing
    /// </remarks>
    public static void Write(Stream output, W3StringsFile file, uint version)
    {
        var storage = W3StringsStorage.Of(file, version); // The whole layout, settled before the first byte

        output.Write(storage.Preamble); // The head, both blocks and the size of the string buffer

        // The texts are laid out in the order the entries were laid out in, so the buffer takes them one
        // after the other, each closed by the terminator the format asks for.
        foreach (var entry in storage.Strings) output.Write(storage.BufferBytesOf(entry.Value));

        output.Write(storage.Tail); // The tail half of the language key, which closes the container
    }
}