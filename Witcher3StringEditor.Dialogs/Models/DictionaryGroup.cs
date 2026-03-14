using System.Globalization;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A class representing a group of dictionaries
/// </summary>
public class DictionaryGroup(CultureInfo targetLanguage, List<DictionaryInfo> dictionaries)
{
    /// <summary>
    ///     Initializes a new instance of the DictionaryGroup class
    /// </summary>
    public CultureInfo TargetLanguage { get; } = targetLanguage;

    /// <summary>
    ///     Initializes a new instance of the DictionaryGroup class
    /// </summary>
    public List<DictionaryInfo> Dictionaries { get; } = dictionaries;
}