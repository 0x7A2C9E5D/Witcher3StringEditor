namespace Witcher3StringEditor.Serializers.W3Strings;

/// <summary>
///     One string of a container: the id it is stored under, its text, and its slot in the string buffer.
/// </summary>
/// <remarks>
///     The fields are filled in the order the container holds them: the slot is needed before the text can
///     be fetched, and the id stays obfuscated until the language key is known.
/// </remarks>
internal sealed class W3StringEntry
{
    /// <summary>
    ///     The id the text is stored under, which a key of the key block resolves to.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    ///     The decoded text.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     The offset of the text in the string buffer, in the units the container counts in.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    ///     The length of the text in the string buffer, in the units the container counts in.
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}