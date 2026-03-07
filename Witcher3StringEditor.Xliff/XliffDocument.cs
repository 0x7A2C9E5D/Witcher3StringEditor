using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff
{
    [PublicAPI]
    public class XliffDocument
    {
        public required XliffInfo Info { get; set; }

        public IReadOnlyDictionary<string, string>? Translations { get; set; }
    }
}
