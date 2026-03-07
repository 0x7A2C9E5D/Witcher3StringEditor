using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff;

[PublicAPI]
public record XliffInfo
{
    public required string FilePath { get; set; }
    
    public required Version Version { get; init; }

    public required string SourceLanguage { get; init; }

    public required string TargetLanguage { get; init; }
    
    public int Count { get; set; }
}