namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Writes the stored form of a container into a stream, every section in the order the format holds them.
/// </summary>
/// <remarks>
///     The layout is settled when the stored form is laid out, before the first byte is written, because the
///     block of offsets and lengths sits before the string buffer it points into. It is offered as an
///     extension of the stored form so that it reads as an operation on it, and so the model never holds a
///     stream of its own.
/// </remarks>
internal static class W3StringsWriter
{
    /// <summary>
    ///     Writes a stored form into the given stream.
    /// </summary>
    /// <param name="storage">The stored form to write, which <see cref="W3StringsStorage.Of" /> laid out.</param>
    /// <param name="output">The stream the container is written to.</param>
    /// <remarks>
    ///     The version is not a parameter: it was chosen when the stored form was laid out, and it is what
    ///     decided how the texts are encoded and how their offsets, lengths and buffer size are counted. A
    ///     version below the UTF-8 generation was laid out as 162.
    /// </remarks>
    public static void Write(this W3StringsStorage storage, Stream output)
    {
        output.Write(storage.Preamble); // The head, both blocks and the size of the string buffer

        // The texts are laid out in entry order, each closed by the terminator the format asks for.
        foreach (var entry in storage.Strings) output.Write(storage.BufferBytesOf(entry.Value));

        output.Write(storage.Tail); // The tail half of the language key, which closes the container
    }
}