using System.Globalization;

namespace Witcher3StringEditor.Dictionary;

/// <summary>
///     A class that represents a dictionary info.
/// </summary>
/// <param name="Path"></param>
/// <param name="Version"></param>
/// <param name="SourceLanguage"></param>
/// <param name="TargetLanguage"></param>
/// <param name="TermCount"></param>
public record DictionaryInfo(
    string Path,
    Version Version,
    CultureInfo SourceLanguage,
    CultureInfo TargetLanguage,
    int TermCount);