namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Reads a container out of the stream its stored form was read from.
/// </summary>
/// <remarks>
///     The stored form is <see cref="W3StringsStorage" />, which says where every text sits; fetching the
///     texts is what turns it into the container the editor works with. It is offered as an extension of the
///     stored form so that it reads as an operation on it, and so the model never holds a stream of its own.
/// </remarks>
internal static class W3StringsReader
{
    /// <summary>
    ///     Reads the texts of a stored form out of the string buffer of the stream it was read from, and
    ///     answers the container they make.
    /// </summary>
    /// <param name="storage">The stored form to read the container of.</param>
    /// <param name="input">The stream the stored form was read from, which holds the string buffer.</param>
    /// <returns>The container, which holds the texts and the keys that resolve them.</returns>
    /// <exception cref="W3StringsException">Thrown when the stored bytes of a text cannot be read.</exception>
    /// <remarks>
    ///     The texts are fetched one at a time, in the order they occupy the buffer, so the container is never
    ///     held whole: only the decoded texts are kept.
    /// </remarks>
    public static W3StringsFile ToContainer(this W3StringsStorage storage, Stream input)
    {
        try
        {
            var storedTexts = ReadTexts(input, storage); // The stored bytes of every text, in entry order
            return Assemble(storage, storedTexts);
        }
        catch (Exception ex) when (ex is not W3StringsException)
        {
            // The codec answers with one exception type, with whatever went wrong below as its cause.
            throw new W3StringsException(W3StringsStorage.UnreadableStream, ex);
        }
    }

    /// <summary>
    ///     Reads the stored bytes of every text out of the string buffer.
    /// </summary>
    /// <param name="input">The stream to read from.</param>
    /// <param name="storage">The stored form, which says where every text sits.</param>
    /// <returns>The stored bytes of every text, in entry order.</returns>
    private static byte[][] ReadTexts(Stream input, W3StringsStorage storage)
    {
        var storedTexts = new byte[storage.Strings.Count][];
        var cursor = storage.BufferOffset;
        // The stored form was read out of order, so the stream is at the tail half of the language key:
        // the buffer is sought first, and the texts move forward from there.
        input.Seek(cursor, SeekOrigin.Begin);

        // Texts are fetched in the order they occupy the buffer, so the stream mostly keeps moving forward.
        // Every one of them was checked against the buffer before this.
        foreach (var i in Enumerable.Range(0, storage.Strings.Count).OrderBy(index => storage.Strings[index].Offset))
        {
            var entry = storage.Strings[i];
            var at = storage.ByteOffsetOf(entry);
            if (at < cursor) input.Seek(at, SeekOrigin.Begin); // An entry out of order has to be sought
            else Skip(input, at - cursor); // A gap is read past, which keeps the read sequential

            var stored = new byte[storage.ByteLengthOf(entry)]; // The buffer was checked to hold every one
            input.ReadExactly(stored);
            storedTexts[i] = stored;
            cursor = at + stored.Length;
        }

        return storedTexts;
    }

    /// <summary>
    ///     Assembles the container out of its stored form, decoding every id and text on the way.
    /// </summary>
    /// <param name="storage">The stored form the container is read from.</param>
    /// <param name="storedTexts">The stored bytes of every text, in entry order.</param>
    /// <returns>The container.</returns>
    private static W3StringsFile Assemble(W3StringsStorage storage, byte[][] storedTexts)
    {
        var file = new W3StringsFile { Key = storage.Key };

        // The id is decoded and the text fetched, turning each entry of the block into the entry the
        // container holds. Where a text sits and how long it was said to be stayed on the entry.
        for (var i = 0; i < storage.Strings.Count; i++)
        {
            var entry = storage.Strings[i];
            entry.Id = storage.IdOf(entry);
            entry.Value = storage.TextOf(entry, storedTexts[i]);
            file.Strings.Add(entry);
        }

        // The key ids are decoded the same way the string ids are.
        foreach (var key in storage.Keys)
            file.Keys.Add(new W3KeyEntry { KeyHash = key.KeyHash, Id = storage.IdOf(key) });

        return file;
    }

    /// <summary>
    ///     Reads past a gap in the string buffer.
    /// </summary>
    /// <param name="input">The stream to read from.</param>
    /// <param name="count">The number of bytes to read past.</param>
    private static void Skip(Stream input, long count)
    {
        if (count <= 0) return;

        var scratch = new byte[(int)Math.Min(count, 4096)];
        while (count > 0)
        {
            var chunk = (int)Math.Min(count, scratch.Length);
            input.ReadExactly(scratch, 0, chunk);
            count -= chunk;
        }
    }
}