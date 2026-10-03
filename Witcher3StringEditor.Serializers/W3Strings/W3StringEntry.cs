namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     One string of a container: the id it is stored under, its text, and the slot of the string buffer
///     the text occupies
/// </summary>
/// <remarks>
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
    public uint Id { get; set; }

    /// <summary>
    ///     The decoded text
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     The offset the text is stored at in the string buffer, in the units the container counts in
    /// </summary>
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