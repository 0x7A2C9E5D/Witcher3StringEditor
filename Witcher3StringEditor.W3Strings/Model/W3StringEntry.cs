namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringEntry
{
    public uint Id { get; init; }

    public uint Offset { get; init; }

    public uint Length { get; init; }

    public string Value { get; init; } = string.Empty;

    /// <summary>
    ///     The bytes of the text as the container stores them: obfuscated with the language magic
    /// </summary>
    /// <remarks>
    ///     Only an entry that was read from a container carries them. They are kept so that writing the
    ///     container back reproduces the very bytes it was read with
    /// </remarks>
    public byte[]? StoredBytes { get; init; }

    /// <summary>
    ///     The bytes of the text with the obfuscation of the language magic undone
    /// </summary>
    /// <remarks>
    ///     Only an entry that was read from a container carries them, and they always accompany
    ///     <see cref="StoredBytes" />: the pair is the whole of what a parsed entry remembers
    /// </remarks>
    public byte[]? PlainBytes { get; init; }

    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}
