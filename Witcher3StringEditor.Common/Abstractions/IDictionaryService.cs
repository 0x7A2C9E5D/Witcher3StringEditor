using System.Collections.ObjectModel;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Common.Abstractions;

public interface IDictionaryService
{
    public ObservableCollection<XliffInfo> Dictionaries { get; }

    public XliffDocument LoadDictionary(XliffInfo xliffInfo);
}