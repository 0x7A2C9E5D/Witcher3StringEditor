using System.Collections.ObjectModel;
using System.IO;
using NReco.Text;
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

    private readonly AhoCorasickDoubleArrayTrie<int> matcher;

    private Dictionary<string, string> terms;

    public DictionaryService()
    {
        terms = new Dictionary<string, string>();
        matcher = new AhoCorasickDoubleArrayTrie<int>();
        dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries");
        if (!Directory.Exists(dictionaryPath))
            Directory.CreateDirectory(dictionaryPath);
        LoadDictionariesFromDirectory(dictionaryPath);
    }

    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    /// <summary>
    ///     Loads a dictionary file and builds term cache with compiled regex
    /// </summary>
    public void LoadDictionary(XliffInfo xliffInfo)
    {
        var doc = xliffReader.ReadDocument(xliffInfo);
        if (doc.Translations is { Count: > 0 })
        {
            terms = doc.Translations.ToDictionary();
            matcher.Build(doc.Translations.ToDictionary(kvp => kvp.Key, _ => 0));
        }
        else
        {
            terms.Clear();
        }
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

    /// <summary>
    ///     Applies dynamic dictionary to text, wrapping matched terms with Microsoft Translator tags
    ///     Format: &lt;mstrans:dictionary translation="translation"&gt;phrase&lt;/mstrans:dictionary&gt;
    /// </summary>
    public string ApplyDynamicDictionary(string text)
    {
        if (string.IsNullOrEmpty(text) || terms.Count == 0)
            return text;

        var result = text;
        matcher.ParseText(text, hit =>
        {
            var phrase = text.Substring(hit.Begin, hit.Length);
            if (terms.TryGetValue(phrase, out var translation))
                result = $"<mstrans:dictionary translation=\"{translation}\">{phrase}</mstrans:dictionary>";
        });

        return result;
    }
    
    private void LoadDictionariesFromDirectory(string path)
    {
        var files = Directory.GetFiles(path).Where(x => x.EndsWith(".xliff") || x.EndsWith(".xlf"));
        files.ForEach(file =>
        {
            var info = xliffReader.ReadInfo(file);
            if (info != null) Dictionaries.Add(info);
        });
    }
}