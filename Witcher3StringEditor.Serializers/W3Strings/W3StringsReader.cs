namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     Reads a w3strings container out of a stream
///     The layout of the container is the business of <see cref="W3StringsStorage" />, which reads the stored
///     form off the stream; this fetches the stored bytes of every text from it and turns the stored form
///     into the container the rest of the editor works with
/// </summary>
/// <remarks>
///     The container is never held whole: <see cref="W3StringsStorage" /> records where every text sits, so
///     the stored bytes are fetched one at a time and only the decoded texts are kept
/// </remarks>
internal static class W3StringsReader
{
    /// <summary>
    ///     Reads a container from a file stream
    /// </summary>
    /// <param name="input">The stream to read from, which a caller opens over a file</param>
    /// <returns>
    ///     The container, which holds the texts and their keys, and the version its texts are stored in:
    ///     the file declares the version, and the container holds none of its own, because the same texts
    ///     are written for either generation of the game
    /// </returns>
    /// <exception cref="W3StringsException">Thrown when the stream does not hold a container this build can decode</exception>
    /// <remarks>
    ///     The version the container is stored in is the one measured against the file, which is the one it
    ///     declares unless it declares one generation and holds the other: the caller is told that one
    ///     rather than the declaration the file carries
    /// </remarks>
    public static (W3StringsFile Container, uint Version) Read(Stream input)
    {
        try
        {
            var storage = W3StringsStorage.Read(input); // The stored form, and the version its texts are in
            var storedTexts = ReadTexts(input, storage); // The stored bytes of every text, in slot order
            return (Assemble(storage, storedTexts), storage.Version);
        }
        catch (Exception ex) when (ex is not W3StringsException)
        {
            // The codec answers with one exception type, with whatever went wrong below as its cause.
            throw new W3StringsException("The stream does not hold a readable w3strings container", ex);
        }
    }

    /// <summary>
    ///     Reads the stored bytes of every text out of the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="storage">The stored form, which says where every text sits</param>
    /// <returns>The stored bytes of every text, in entry order</returns>
    private static byte[][] ReadTexts(Stream input, W3StringsStorage storage)
    {
        var storedTexts = new byte[storage.Strings.Count][];
        var cursor = storage.BufferOffset;
        // The stored form was read by reaching out of order, so the stream is at the tail half of the
        // language key: the buffer is sought first, and the texts move forward from there.
        input.Seek(cursor, SeekOrigin.Begin);

        // Texts are fetched in the order they occupy the buffer, which is the order they are listed in
        // for every container this build writes, so the stream simply keeps moving forward. Every one of
        // them was checked against the buffer before this, so none of them reaches past it.
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
    ///     Assembles the container out of its stored form, decoding every id and text on the way
    /// </summary>
    /// <param name="storage">The stored form the container is read from</param>
    /// <param name="storedTexts">The stored bytes of every text, in entry order</param>
    /// <returns>The container</returns>
    private static W3StringsFile Assemble(W3StringsStorage storage, byte[][] storedTexts)
    {
        var file = new W3StringsFile { Key = storage.Key };

        // The id is decoded and the text fetched, which is what turns every entry of the block into the
        // entry the container holds. Where a text sits and how long it was said to be stay on the entry:
        // they are what the block stores, and a writer records them the same way as it lays the buffer out.
        for (var i = 0; i < storage.Strings.Count; i++)
        {
            var entry = storage.Strings[i];
            entry.Id = storage.IdOf(entry);
            entry.Value = storage.TextOf(entry, storedTexts[i]);
            file.Strings.Add(entry);
        }

        // The ids of the keys are decoded the way those of the strings are, which is what makes every
        // entry of the container hold what it says it holds.
        foreach (var key in storage.Keys)
            file.Keys.Add(new W3KeyEntry { KeyHash = key.KeyHash, Id = storage.IdOf(key) });

        return file;
    }

    /// <summary>
    ///     Reads past a gap in the string buffer
    /// </summary>
    /// <param name="input">The stream to read from</param>
    /// <param name="count">The number of bytes to read past</param>
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