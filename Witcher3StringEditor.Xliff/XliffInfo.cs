namespace Witcher3StringEditor.Xliff;

public class XliffInfo
{
    public required Version Version { get; init; }

    public required string SourceLanguage { get; init; }

    public required string TargetLanguage { get; init; }
}