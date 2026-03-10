using System.Collections.ObjectModel;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Common.Abstractions;

public interface IDictionaryService
{
    /// <summary>
    ///     Dictionary files
    /// </summary>
    public ObservableCollection<XliffInfo> Dictionaries { get; }

    /// <summary>
    ///     None dictionary
    /// </summary>
    public XliffInfo NoneDictionary { get; }

    /// <summary>
    ///     Loads a dictionary file and builds term cache with compiled regex
    /// </summary>
    public void LoadDictionary(XliffInfo xliffInfo);

    /// <summary>
    ///     Adds a dictionary file to the collection
    /// </summary>
    public void AddDictionaryFromFile(string path);

    /// <summary>
    ///     Removes a dictionary file and removes it from the collection
    /// </summary>
    public void RemoveDictionary(XliffInfo xliffInfo);

    /// <summary>
    ///     Applies dynamic dictionary to text, wrapping matched terms with Microsoft Translator tags
    ///     Format: &lt;mstrans:dictionary translation="translation"&gt;phrase&lt;/mstrans:dictionary&gt;
    /// </summary>
    public string ApplyDynamicDictionary(string text);
}