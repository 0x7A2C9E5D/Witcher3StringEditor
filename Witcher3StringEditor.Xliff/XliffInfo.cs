using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff;

[PublicAPI]
public class XliffInfo
{
    public required string FilePath { get; init; }
    
    public required Version Version { get; init; }

    public required string SourceLanguage { get; init; }

    public required string TargetLanguage { get; init; }
}