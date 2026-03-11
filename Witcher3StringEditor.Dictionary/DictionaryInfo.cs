using System.Globalization;

namespace Witcher3StringEditor.Dictionary;

public record DictionaryInfo(
    string Path,
    Version Version,
    CultureInfo SourceLanguage,
    CultureInfo TargetLanguage,
    int TermCount);