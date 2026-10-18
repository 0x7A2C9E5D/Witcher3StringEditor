namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Writes a container into a stream, every section in the order the format holds them.
/// </summary>
/// <remarks>
///     The layout is settled by <see cref="W3StringsStorage" /> before the first byte is written, since the
///     block of offsets and lengths precedes the string buffer it points into.
/// </remarks>
internal static class W3StringsWriter
{
    /// <summary>
    ///     Writes a container into the given stream.
    /// </summary>
    /// <param name="output">The stream the container is written to.</param>
    /// <param name="file">The container to encode.</param>
    /// <param name="version">The version to write it with, chosen by the caller for the game it is for.</param>
    /// <remarks>
    ///     The version decides how the texts are encoded and how their offsets, lengths and buffer size are
    ///     counted. A version below the UTF-8 generation is written as 162.
    /// </remarks>
    public static void Write(Stream output, W3StringsFile file, uint version)
    {
        var storage = W3StringsStorage.Of(file, version); // The whole layout, settled before the first byte

        output.Write(storage.Preamble); // The head, both blocks and the size of the string buffer

        // The texts are laid out in entry order, each closed by the terminator the format asks for.
        foreach (var entry in storage.Strings) output.Write(storage.BufferBytesOf(entry.Value));

        output.Write(storage.Tail); // The tail half of the language key, which closes the container
    }
}