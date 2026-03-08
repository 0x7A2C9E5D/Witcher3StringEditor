using System.Collections.ObjectModel;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Common.Abstractions;

public interface IDictionaryService
{
    public ObservableCollection<XliffInfo> Dictionaries { get; }

    public void LoadDictionary(XliffInfo xliffInfo);

    public void AddDictionaryFromFile(string path);

    public void RemoveDictionary(XliffInfo xliffInfo);

    public string ApplyDynamicDictionary(string text);
}