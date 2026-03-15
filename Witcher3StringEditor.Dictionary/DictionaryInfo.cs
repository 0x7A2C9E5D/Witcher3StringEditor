using System.Globalization;

namespace Witcher3StringEditor.Dictionary;

/// <summary>
///     A class that represents a dictionary info.
/// </summary>
/// <param name="Name"></param>
/// <param name="Note"></param>
/// <param name="SourceLanguage"></param>
/// <param name="TargetLanguage"></param>
/// <param name="TermCount"></param>
public record DictionaryInfo(
    string Path,
    string Name,
    string Note,
    CultureInfo SourceLanguage,
    CultureInfo TargetLanguage,
    int TermCount);