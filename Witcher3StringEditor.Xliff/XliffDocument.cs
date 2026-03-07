namespace Witcher3StringEditor.Xliff
{
    public class XliffDocument
    {
        public required string Name { get; init; }

        public string? Description { get; init; }

        public required Version Version { get; init; }

        public required string SourceLanguage { get; init; }

        public required string TargetLanguage { get; init; }

        public IReadOnlyDictionary<string, string>? Translations { get; set; }
    }
}
