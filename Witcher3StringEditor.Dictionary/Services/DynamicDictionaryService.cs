using Cysharp.Text;
using NReco.Text;
using Serilog;
using Witcher3StringEditor.Dictionary.Providers;

namespace Witcher3StringEditor.Dictionary.Services;

public class DynamicDictionaryService(IDictionaryProvider provider) : IDynamicDictionaryService
{
    private readonly AhoCorasickDoubleArrayTrie<int> matcher = new(); // Create term cache
    private Dictionary<string, string> entries = []; // Create entries

    /// <summary>
    ///     A service that provides dynamic dictionary functionality.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>
    ///     A service that provides dynamic dictionary functionality.
    /// </summary>
    public DictionaryInfo? CurrentDictionary { get; private set; }

    /// <summary>
    ///     Binds the dynamic dictionary to the given dictionary.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <returns></returns>
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

    /// <summary>
    ///     Replaces all matches in the given text with their corresponding translations from the dynamic dictionary.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public string Replace(string text)
    {
        if (string.IsNullOrEmpty(text) || entries.Count == 0) return text;

        var hits = FindAndSortMatches(text, matcher);
        hits = FilterValidHits(hits, text.Length);
        return ReplaceMatches(text, hits, entries);
    }

    /// <summary>
    ///     Finds all matches in the given text and returns them sorted by position and length.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="matcher"></param>
    /// <returns></returns>
    private static List<AhoCorasickDoubleArrayTrie<int>.Hit> FindAndSortMatches(string text,
        AhoCorasickDoubleArrayTrie<int> matcher)
    {
        var allHits = new List<AhoCorasickDoubleArrayTrie<int>.Hit>(); // Create list to store hits

        matcher.ParseText(text, hit => { allHits.Add(hit); }); // Find all hits

        return allHits
            .OrderBy(x => x.Begin)
            .ThenByDescending(x => x.Length)
            .ToList(); // Sort hits
    }

    /// <summary>
    ///     Filters out invalid hits from the given list of hits.
    /// </summary>
    /// <param name="hits"></param>
    /// <param name="textLength"></param>
    /// <returns></returns>
    private static List<AhoCorasickDoubleArrayTrie<int>.Hit> FilterValidHits(
        IReadOnlyList<AhoCorasickDoubleArrayTrie<int>.Hit> hits, int textLength)
    {
        var occupied = new bool[textLength]; // Create array to track occupied characters
        var validHits = new List<AhoCorasickDoubleArrayTrie<int>.Hit>(); // Create list to store valid hits
        foreach (var hit in hits) // Iterate through hits
        {
            var isFree = true; // Initialize flag
            for (var i = hit.Begin; i < hit.End; i++) // Iterate through characters
            {
                if (!occupied[i]) continue; // Check if character is free
                isFree = false; // Set flag
                break; // Break
            }

            if (!isFree) continue; // Check if hit is valid
            validHits.Add(hit); // Add hit
            Array.Fill(occupied, true, hit.Begin, hit.Length); // Mark characters as occupied
        }

        return validHits; // Return valid hits
    }

    /// <summary>
    ///     Replaces all matches in the given text with their corresponding translations from the dynamic dictionary.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="hits"></param>
    /// <param name="entries"></param>
    /// <returns></returns>
    private static string ReplaceMatches(string text, List<AhoCorasickDoubleArrayTrie<int>.Hit> hits,
        Dictionary<string, string> entries)
    {
        var currentPos = 0; // Initialize current position
        using var stringBuilder = ZString.CreateStringBuilder(); // Create string builder
        foreach (var hit in hits) // Iterate through hits
        {
            if (hit.Begin > currentPos)
                stringBuilder.Append(text.AsSpan(currentPos, hit.Begin - currentPos)); // Append text before hit
            var phrase = text.Substring(hit.Begin, hit.Length); // Extract matched phrase
            var translation = entries[phrase]; // Get translation from entries
            stringBuilder.Append("<mstrans:dictionary translation='"); // Append opening tag with translation attribute
            stringBuilder.Append(EscapeXml(translation)); // Append translation
            stringBuilder.Append("'>"); // Append closing tag
            stringBuilder.Append(EscapeXml(phrase)); // Append original phrase
            stringBuilder.Append("</mstrans:dictionary>"); // Append closing tag
            currentPos = hit.End; // Move current position
        }

        if (currentPos < text.Length) stringBuilder.Append(text.AsSpan(currentPos)); // Append remaining text

        return stringBuilder.ToString(); // Return replaced text
    }

    /// <summary>
    ///     Escapes XML special characters in the given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;")
            .Replace("\"", "&quot;");
    }
}