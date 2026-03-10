using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using NReco.Text;
using Serilog;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Common.Abstractions;
using Witcher3StringEditor.Xliff;

namespace Witcher3StringEditor.Services;

public class DictionaryService : IDictionaryService
{
#if DEBUG
    private static bool IsDebug => true; // Debug
#else
    private static bool IsDebug => false; // Release
#endif

    private readonly AhoCorasickDoubleArrayTrie<int> matcher; // Aho-Corasick
    private readonly XliffReader xliffReader = new(); // Xliff reader
    private Dictionary<string, string> terms = []; // Terms
    private readonly string dictionaryPath; // Dictionary path

    /// <summary>
    ///     None dictionary
    /// </summary>
    public XliffInfo NoneDictionary { get; } = new()
    {
        FilePath = string.Empty,
        Version = new Version(1, 0),
        SourceLanguage = CultureInfo.InvariantCulture,
        TargetLanguage = CultureInfo.InvariantCulture,
        TermCount = 0
    };

    /// <summary>
    ///     Dictionary service
    /// </summary>
    public DictionaryService()
    {
        matcher = new AhoCorasickDoubleArrayTrie<int>(); // Aho-Corasick
        dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries"); // Dictionary path
        if (!Directory.Exists(dictionaryPath)) // If directory does not exist
            Directory.CreateDirectory(dictionaryPath); // Create directory
        LoadDictionariesFromDirectory(dictionaryPath); // Load dictionaries from directory
    }

    /// <summary>
    ///     Dictionary files
    /// </summary>
    public ObservableCollection<XliffInfo> Dictionaries { get; } = [];

    /// <summary>
    ///     Loads a dictionary file and builds term cache with compiled regex
    /// </summary>
    public void LoadDictionary(XliffInfo xliffInfo)
    {
        var xliffDocument = xliffReader.ReadDocument(xliffInfo); // Read document
        if (xliffDocument.Translations is { Count: > 0 }) // If translations exists
        {
            terms = xliffDocument.Translations
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(); // Create dictionary
            matcher.Build(xliffDocument.Translations.ToDictionary(kvp => kvp.Key, _ => 0)); // Build term cache
        }
        else
        {
            terms.Clear(); // Clear
        }
    }

    /// <summary>
    ///     Adds a dictionary file to the collection
    /// </summary>
    public void AddDictionaryFromFile(string path)
    {
        var xliffInfo = xliffReader.ReadInfo(path); // Read info
        if (xliffInfo == null) return; // No info
        var destFileName =
            Path.Combine(dictionaryPath, Path.GetFileName(xliffInfo.FilePath)); // Get destination file name
        if (Dictionaries.Any(x => x.FilePath.Equals(destFileName))) return; // Already exists
        File.Copy(xliffInfo.FilePath, destFileName); // Copy file
        xliffInfo.FilePath = destFileName; // Set file path
        Dictionaries.Add(xliffInfo); // Add to collection
    }

    /// <summary>
    ///     Removes a dictionary file and removes it from the collection
    /// </summary>
    public void RemoveDictionary(XliffInfo xliffInfo)
    {
        if (!Dictionaries.Contains(xliffInfo)) return; // Not found
        Dictionaries.Remove(xliffInfo); // Remove from collection
        File.Delete(xliffInfo.FilePath); // Delete the file
    }

    private const string DynamicDictionaryTemplate =
        @"'<mstrans:dictionary translation='{0}'>{1}</mstrans:dictionary>'"; // Tag

    /// <summary>
    ///     Applies dynamic dictionary to text, wrapping matched terms with Microsoft Translator tags
    ///     Format: &lt;mstrans:dictionary translation="translation"&gt;phrase&lt;/mstrans:dictionary&gt;
    /// </summary>
    public string ApplyDynamicDictionary(string text)
    {
        if (string.IsNullOrEmpty(text) || terms.Count == 0) return text; // No text or no terms

        var processedText = text;
        matcher.ParseText(text, hit =>
        {
            var phrase = text.Substring(hit.Begin, hit.Length); // Get phrase
            if (terms.TryGetValue(phrase, out var translation)) // If translation exists
                processedText =
                    string.Format(DynamicDictionaryTemplate, translation, phrase); // Replace phrase with tag
        });

        return processedText;
    }

    /// <summary>
    ///     Loads all dictionaries from directory
    /// </summary>
    /// <param name="path"></param>
    private void LoadDictionariesFromDirectory(string path)
    {
        try
        {
            var dictionaryFiles = Directory.GetFiles(path)
                .Where(f => f.EndsWith(".xliff") || f.EndsWith(".xlf")); // Get all xliff files
            dictionaryFiles.ForEach(dictionaryFile =>
            {
                var xliffInfo = xliffReader.ReadInfo(dictionaryFile); // Read info
                if (xliffInfo != null) Dictionaries.Add(xliffInfo); // Add to dictionary
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error loading dictionaries from directory: {Path}", path);
        }
    }
}