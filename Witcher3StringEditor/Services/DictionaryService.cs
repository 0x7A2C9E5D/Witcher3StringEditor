using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
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
    
    /// <summary>
    /// Compiled terms from loaded dictionary
    /// </summary>
    private Dictionary<string, string> terms = new();
    
    /// <summary>
    /// Cached compiled regex
    /// </summary>
    private Regex? regex;


    public DictionaryService()
    {
        dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries");
        if (!Directory.Exists(dictionaryPath))
            Directory.CreateDirectory(dictionaryPath);
        LoadDictionariesFromDirectory(dictionaryPath);
    }

    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    /// <summary>
    /// Loads a dictionary file and builds term cache with compiled regex
    /// </summary>
    public void LoadDictionary(XliffInfo xliffInfo)
    {
        var doc = xliffReader.ReadDocument(xliffInfo);
        
        if (doc.Translations != null && doc.Translations.Count > 0)
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
    /// Applies dynamic dictionary to text, wrapping matched terms with Microsoft Translator tags
    /// Format: &lt;mstrans:dictionary translation="translation"&gt;phrase&lt;/mstrans:dictionary&gt;
    /// </summary>
    public string ApplyDynamicDictionary(string text)
    {
        if (string.IsNullOrEmpty(text) || terms.Count == 0 || regex == null)
            return text;

        return regex.Replace(text, match =>
        {
            var key = terms.Keys.FirstOrDefault(k => 
                string.Equals(k, match.Value, StringComparison.OrdinalIgnoreCase));
            
            if (key == null) return match.Value;
            
            // Direct string formatting to produce exact output format
            var translation = EscapeXml(terms[key]);
            var phrase = EscapeXml(match.Value);
            return $"<mstrans:dictionary translation=\"{translation}\">{phrase}</mstrans:dictionary>";
        });
    }

    private static Regex CreateCompiledRegex(Dictionary<string, string> terms)
    {
        var sorted = terms.Keys.OrderByDescending(k => k.Length).ToList();
        var pattern = sorted.Count > 0 ? @"\b(" + string.Join("|", sorted.Select(Regex.Escape)) + @")\b" : string.Empty;
        
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

    /// <summary>
    /// Escapes XML special characters in attribute values and text content
    /// </summary>
    private static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }
}
