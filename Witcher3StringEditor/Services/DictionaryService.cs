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

    private readonly XliffReader xliffReader = new();
    private readonly string dictionaryPath;


    public DictionaryService()
    {
        dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries");
        if (!Directory.Exists(dictionaryPath))
            Directory.CreateDirectory(dictionaryPath);
        LoadDictionariesFromDirectory(dictionaryPath);
    }

    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    public XliffDocument LoadDictionary(XliffInfo xliffInfo)
    {
        return xliffReader.ReadDocument(xliffInfo);
    }

    public void AddDictionaryFromFile(string path)
    {
        var xliffInfo = xliffReader.ReadInfo(path);
        if (xliffInfo == null) return;
        var destFileName = Path.Combine(dictionaryPath, Path.GetFileName(xliffInfo.FilePath));
        if (Dictionaries.Any(x => x.FilePath.Equals(destFileName))) return;
        File.Copy(xliffInfo.FilePath, destFileName);
        xliffInfo.FilePath = destFileName;
        Dictionaries.Add(xliffInfo);
    }

    public void RemoveDictionary(XliffInfo xliffInfo)
    {
        if (!Dictionaries.Contains(xliffInfo)) return;
        Dictionaries.Remove(xliffInfo);
        File.Delete(xliffInfo.FilePath);
    }

    private void LoadDictionariesFromDirectory(string path)
    {
        var files = Directory.GetFiles(path)
            .Where(x => x.EndsWith(".xliff") || x.EndsWith(".xlf"));
        files.ForEach(file =>
        {
            var info = xliffReader.ReadInfo(file);
            if (info != null) Dictionaries.Add(info);
        });
    }
}