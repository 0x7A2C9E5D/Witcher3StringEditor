using System.Collections.ObjectModel;
using System.Globalization;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A class representing a group of dictionaries that share a target language
/// </summary>
/// <param name="targetLanguage">The target language of the dictionaries in this group</param>
/// <param name="dictionaries">The dictionaries of this group</param>
public class DictionaryGroup(CultureInfo targetLanguage, List<DictionaryInfo> dictionaries)
{
    /// <summary>
    ///     Gets the target language of the dictionaries in this group
    /// </summary>
    public CultureInfo TargetLanguage { get; } = targetLanguage;

    /// <summary>
    ///     Gets the dictionaries of this group as an observable collection
    /// </summary>
    public ObservableCollection<DictionaryInfo> Dictionaries { get; } = dictionaries.ToObservableCollection();
}