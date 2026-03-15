using NReco.Text;
using Serilog;
using Witcher3StringEditor.Dictionary.Providers;

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
        if (string.IsNullOrEmpty(text) || entries.Count == 0) return text;
        
        var offset = 0;
        var lastEnd = -1;
        var processedText = text;

        var hitsToProcess = new List<(AhoCorasickDoubleArrayTrie<int>.Hit Hit, string Phrase, string Translation)>();
        
        matcher.ParseText(text, hit =>
        {
            var phrase = text.Substring(hit.Begin, hit.Length);
            
            if (!entries.TryGetValue(phrase, out var translation)) 
                return;
            
            if (IsAlreadyTagged(text, hit.Begin, hit.Length)) 
                return;
            
            if (hit.Begin < lastEnd) 
                return;
            
            hitsToProcess.Add((hit, phrase, translation));
            lastEnd = hit.Begin + hit.Length;
        });
        
        foreach (var (hit, phrase, translation) in hitsToProcess.OrderBy(h => h.Hit.Begin))
        {
            var tag = string.Format(DynamicDictionaryTemplate, translation, phrase);
            var position = hit.Begin + offset;
            
            processedText = processedText.Remove(position, hit.Length);
            processedText = processedText.Insert(position, tag);
            
            offset += tag.Length - hit.Length;
        }

        return processedText;
    }

    /// <summary>
    ///     Checks if the substring at the specified position is already wrapped in a dictionary tag
    /// </summary>
    private bool IsAlreadyTagged(string text, int begin, int length)
    {
        const string openTagPrefix = "<mstrans:dictionary";
        const string closeTagSuffix = "</mstrans:dictionary>";
        
        var checkStart = Math.Max(0, begin - openTagPrefix.Length * 2);
        var checkEnd = Math.Min(text.Length, begin + length + closeTagSuffix.Length * 2);
        
        if (checkEnd <= checkStart) return false;
        
        var context = text.Substring(checkStart, checkEnd - checkStart);
        
        return context.Contains(openTagPrefix) && context.Contains(closeTagSuffix);
    }
}