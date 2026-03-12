using NReco.Text;
using Serilog;

namespace Witcher3StringEditor.Dictionary.Services;

public class DynamicDictionaryService(IDictionaryProvider provider) : IDynamicDictionaryService
{
    private Dictionary<string, string> entries = [];
    private readonly AhoCorasickDoubleArrayTrie<int> matcher = new();

    private const string DynamicDictionaryTemplate =
        @"<mstrans:dictionary translation='{0}'>{1}</mstrans:dictionary>";

    public bool IsReady { get; private set; }
    
    public DictionaryInfo? CurrentDictionary { get; private set; }

    public bool Bind(DictionaryInfo dictionary)
    {
        try
        {
            CurrentDictionary = dictionary;
            entries = provider.GetEntries(dictionary)
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .GroupBy(pair => pair.Key)
                .Select(g=>g.First())
                .ToDictionary();
            matcher.Build(entries.ToDictionary(kvp => kvp.Key, _ => 0)); // Build term cache
            IsReady = true;
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error binding dynamic dictionary");
            CurrentDictionary = null;
            entries.Clear();
            IsReady = false;
            return false;
        }
    }

    public string Replace(string text)
    {
        if (string.IsNullOrEmpty(text) || entries.Count == 0) return text; // No text or no terms
        var processedText = text;
        matcher.ParseText(text, hit =>
        {
            var phrase = text.Substring(hit.Begin, hit.Length); // Get phrase
            if (!entries.TryGetValue(phrase, out var translation)) return; // If translation exists
            var tag = string.Format(DynamicDictionaryTemplate, translation, phrase); // Replace phrase with tag
            processedText = processedText.Remove(hit.Begin, hit.Length);
            processedText = processedText.Insert(hit.Begin, tag);
        });

        return processedText;
    }
}