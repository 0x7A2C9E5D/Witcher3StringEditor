namespace Witcher3StringEditor.Xliff
{
    public class XliffDocument
    {
        public required XliffInfo Info { get; set; }

        public IReadOnlyDictionary<string, string>? Translations { get; set; }
    }
}
