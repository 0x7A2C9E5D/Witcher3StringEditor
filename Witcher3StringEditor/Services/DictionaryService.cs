using System.Collections.ObjectModel;
using System.IO;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Services;

public class DictionaryService : IDictionaryService
{
    private readonly XliffReader xliffReader = new();
    private readonly FileSystemWatcher fileWatcher = new();
    
    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    public XliffDocument LoadDictionary(XliffInfo xliffInfo)
    {
        return xliffReader.ReadDocument(xliffInfo);
    }

    public DictionaryService()
    {
        fileWatcher.Path = string.Empty;
        fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                   NotifyFilters.Size;
        fileWatcher.Filters.Add("*.xlf");
        fileWatcher.Filters.Add("*.xliff");
        fileWatcher.EnableRaisingEvents = true;
    }
}