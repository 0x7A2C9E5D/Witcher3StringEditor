namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringEntry
{
    /// <summary>
    ///     The id the text is stored under, which a key of the key block resolves to
    /// </summary>
    public uint Id { get; init; }

    /// <summary>
    ///     The decoded text, which is the only form of it this model keeps: where the container put it
    ///     and how long it said it was are facts of the encoding, not of the text
    /// </summary>
    public string Value { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}