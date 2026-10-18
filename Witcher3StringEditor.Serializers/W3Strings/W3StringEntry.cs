namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     One string of a container: the id it is stored under, its text, and the slot of the string buffer
///     the text occupies
/// </summary>
/// <remarks>
///     This is the only definition of a string there is: a container holds these entries, and the stored
///     form (<see cref="W3StringsStorage" />) reads and writes the very same ones rather than a second set.
///     The fields are filled in the order the container holds them rather than at construction: the slot of
///     a text is needed before the text can be fetched, and the id is obfuscated until the language key of
///     the container is known, so a reader records the stored facts first and decodes them once the whole
///     container has been read. A writer records the slot of every text as it lays the buffer out
/// </remarks>
internal sealed class W3StringEntry
{
    /// <summary>
    ///     The id the text is stored under, which a key of the key block resolves to
    /// </summary>
    /// <remarks>
    ///     It is held as the container stores it, obfuscated with the magic of the language, until the
    ///     container has been decoded: a reader decodes it in place, and a writer lays the clear id out as
    ///     the container stores it
    /// </remarks>
    public uint Id { get; set; }

    /// <summary>
    ///     The decoded text
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     The offset the text is stored at in the string buffer, in the units the container counts in
    /// </summary>
    /// <remarks>
    ///     It is a fact of the stored form and is recorded there: a reader takes it from the block of
    ///     offsets, and a writer records it as it lays the buffer out
    /// </remarks>
    public uint Offset { get; set; }

    /// <summary>
    ///     The length the text is stored with in the string buffer, in the units the container counts in
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    ///     Returns a string that represents the current object
    /// </summary>
    /// <returns>
    ///     A string that represents the current object
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}
