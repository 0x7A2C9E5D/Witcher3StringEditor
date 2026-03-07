using System.Collections.ObjectModel;
using System.IO;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Services;

public class DictionaryService : IDictionaryService
{
#if DEBUG
    private static bool IsDebug => true;
#else
    private static bool IsDebug => false;
#endif

    private readonly FileSystemWatcher fileWatcher = new();
    private readonly XliffReader xliffReader = new();

    public DictionaryService()
    {
        var dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries");
        if (!Directory.Exists(dictionaryPath)) Directory.CreateDirectory(dictionaryPath);
        LoadDictionariesFromDirectory(dictionaryPath);
        fileWatcher.Path = dictionaryPath;
        fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                   NotifyFilters.Size;
        fileWatcher.Filters.Add("*.xlf");
        fileWatcher.Filters.Add("*.xliff");
        fileWatcher.EnableRaisingEvents = true;
        fileWatcher.Created += FileWatcher_Created;
        fileWatcher.Deleted += FileWatcher_Deleted;
        fileWatcher.Renamed += FileWatcher_Renamed;
    }

    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    public XliffDocument LoadDictionary(XliffInfo xliffInfo)
    {
        return xliffReader.ReadDocument(xliffInfo);
    }

    private void LoadDictionariesFromDirectory(string path)
    {
        var files = Directory.GetFiles(path)
            .Where(x => x.EndsWith(".xliff") || x.EndsWith(".xlf"));
        files.ForEach(file => xliffReader.ReadInfo(file));
    }

    private void FileWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        var xliffInfo = Dictionaries.FirstOrDefault(x => x.FilePath == e.OldFullPath);
        if (xliffInfo != null) xliffInfo.FilePath = e.FullPath;
    }

    private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        var xliffInfo = xliffReader.ReadInfo(e.FullPath);
        if (xliffInfo != null) Dictionaries.Add(xliffInfo);
    }

    private void FileWatcher_Created(object sender, FileSystemEventArgs e)
    {
        var xliffInfo = Dictionaries.FirstOrDefault(x => x.FilePath == e.FullPath);
        if (xliffInfo != null) Dictionaries.Remove(xliffInfo);
    }
}