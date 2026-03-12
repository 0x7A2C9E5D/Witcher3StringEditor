using NReco.Text;
using Serilog;

namespace Witcher3StringEditor.Dictionary.Services;

public class DynamicDictionaryService(IDictionaryProvider provider) : IDynamicDictionaryService
{
    private const string DynamicDictionaryTemplate =
        @"<mstrans:dictionary translation='{0}'>{1}</mstrans:dictionary>";

    private readonly AhoCorasickDoubleArrayTrie<int> matcher = new();
    private Dictionary<string, string> entries = [];

    public bool IsReady { get; private set; }

    public DictionaryInfo? CurrentDictionary { get; private set; }

    public bool Bind(DictionaryInfo dictionary)
    {
        try
        {
            CurrentDictionary = dictionary; // Set current dictionary
            entries = provider.GetEntries(dictionary)
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .GroupBy(pair => pair.Key)
                .Select(g => g.First())
                .ToDictionary();
            matcher.Build(entries.ToDictionary(kvp => kvp.Key, _ => 0)); // Build term cache
            IsReady = true; // Set ready
            return true; // Return success
        }
        catch (Exception e)
        {
            Log.Error(e, "Error binding dynamic dictionary"); // Log error
            CurrentDictionary = null; // Clear current dictionary
            entries.Clear(); // Clear entries
            IsReady = false; // Set not ready
            return false; // Return failure
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
            processedText = processedText.Remove(hit.Begin, hit.Length); // Remove phrase
            processedText = processedText.Insert(hit.Begin, tag); // Insert tag
        });

        return processedText;
    }
}