using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

    private Dictionary<string, string> terms = new();

    /// <summary>
    ///     Cached compiled regex
    /// </summary>
    private Regex? regex;

    public void LoadDictionary(XliffInfo xliffInfo)
    {
        var doc = xliffReader.ReadDocument(xliffInfo);

        if (doc.Translations is { Count: > 0 })
        {
            terms = new Dictionary<string, string>(doc.Translations);
            regex = CreateCompiledRegex(terms);
        }
        else
        {
            terms.Clear();
            regex = null;
        }
    }

    private static Regex CreateCompiledRegex(Dictionary<string, string> terms)
    {
        var sorted = terms.Keys.OrderByDescending(k => k.Length).ToList();
        var pattern = sorted.Count > 0 ? @"\b(" + string.Join("|", sorted.Select(Regex.Escape)) + @")\b" : string.Empty;
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public string ApplyDynamicDictionary(string text)
    {
        if (string.IsNullOrEmpty(text) || terms.Count == 0 || regex == null)
            return text;

        return regex.Replace(text, match =>
        {
            var key = terms.Keys.FirstOrDefault(k =>
                string.Equals(k, match.Value, StringComparison.OrdinalIgnoreCase));

            if (key == null) return match.Value;

            // Use XElement to create properly escaped XML
            var element = new XElement("mstrans:dictionary",
                new XAttribute("translation", terms[key]),
                match.Value
            );

            return element.ToString(SaveOptions.DisableFormatting);
        });
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