using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff
{
    /// <summary>
    ///    XLIFF document
    /// </summary>
    [PublicAPI]
    public class XliffDocument
    {
        /// <summary>
        ///    XLIFF info
        /// </summary>
        public required XliffInfo Info { get; set; }

        /// <summary>
        ///    Translations
        /// </summary>
        public IReadOnlyDictionary<string, string>? Translations { get; set; }
    }
}
