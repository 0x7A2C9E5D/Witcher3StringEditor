namespace Witcher3StringEditor.W3Strings.Model;

public sealed class W3StringEntry
{
    public uint Id { get; init; }

    public uint Offset { get; init; }

    public uint Length { get; init; }

    public string Value { get; init; } = string.Empty;

    public byte[]? StoredBytes { get; init; }

    public byte[]? PlainBytes { get; init; }

    public string? OriginalValue { get; init; }

    public override string ToString()
    {
        return $"{Id}: {Value}";
    }
}