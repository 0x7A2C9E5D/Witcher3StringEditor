using System.Collections.ObjectModel;
using System.IO;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Services;

public class DictionaryService : IDictionaryService
{
    private readonly FileSystemWatcher fileWatcher = new();
    private readonly XliffReader xliffReader = new();

    public DictionaryService()
    {
        fileWatcher.Path = string.Empty;
        fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                   NotifyFilters.Size;
        fileWatcher.Filters.Add("*.xlf");
        fileWatcher.Filters.Add("*.xliff");
        fileWatcher.EnableRaisingEvents = true;
        fileWatcher.Changed += FileWatcher_Changed;
    }

    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    public XliffDocument LoadDictionary(XliffInfo xliffInfo)
    {
        return xliffReader.ReadDocument(xliffInfo);
    }

    private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                HandleFileCreated(e);
                break;
            case WatcherChangeTypes.Deleted:
                HandleFileDeleted(e);
                break;
            case WatcherChangeTypes.Changed:
            case WatcherChangeTypes.Renamed:
            case WatcherChangeTypes.All:
            default:
                break;
        }
    }

    private void HandleFileDeleted(FileSystemEventArgs e)
    {
        var xliffInfo = Dictionaries.FirstOrDefault(x => x.FilePath == e.FullPath);
        if (xliffInfo != null) Dictionaries.Remove(xliffInfo);
    }

    private void HandleFileCreated(FileSystemEventArgs e)
    {
        var xliffInfo = xliffReader.ReadInfo(e.FullPath);
        if (xliffInfo != null) Dictionaries.Add(xliffInfo);
    }
}