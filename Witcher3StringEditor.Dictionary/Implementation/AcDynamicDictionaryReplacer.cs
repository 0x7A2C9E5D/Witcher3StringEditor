using System.Globalization;
using System.Text;
using NReco.Text;
using Serilog;
using Witcher3StringEditor.Dictionary.Abstractions;

namespace Witcher3StringEditor.Dictionary.Implementation;

/// <summary>
///     A service that provides dynamic dictionary functionality.
/// </summary>
/// <param name="provider"></param>
public class AcDynamicDictionaryReplacer(IDictionaryProvider provider) : IDynamicDictionaryReplacer
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
    public async Task<bool> Bind(DictionaryInfo dictionary)
    {
        try
        {
            var rawEntries
                = await provider.GetEntries(dictionary);
            entries = rawEntries
                .AsParallel()
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase
                );
            var terms = entries.ToDictionary(kvp => kvp.Key, _ => 0);
            await Task.Run(() => matcher.Build(terms)); // Build term cache
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
        if (string.IsNullOrWhiteSpace(text) || entries.Count == 0)
            return text; // Return original text if it's empty or if there are no entries

        var hits = FindAndSortMatches(text, matcher); // Find and sort matches
        hits = FilterValidHits(hits, text.Length); // Filter out invalid hits
        return ReplaceMatches(text, hits, entries); // Replace matches with translations
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
        var occupied = new bool[textLength]; // Rent array from pool

        var validHits = new List<AhoCorasickDoubleArrayTrie<int>.Hit>(); // Create list to store valid hits
        foreach (var hit in hits) // Iterate through hits
        {
            var slice = occupied.AsSpan(hit.Begin, hit.Length); // Get slice of occupied array
            if (slice.Contains(true)) continue; // Skip hits that overlap
            slice.Fill(true); // Mark characters as occupied
            validHits.Add(hit); // Add hit
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
        var stringBuilder = new StringBuilder();

        foreach (var hit in hits)
        {
            // Append text before match
            if (hit.Begin > currentPos)
                stringBuilder.Append(text.AsSpan(currentPos, hit.Begin - currentPos));

            // Append match
            var phraseSpan = text.AsSpan(hit.Begin, hit.Length);
            var phrase = phraseSpan.ToString();
            var translation = entries[phrase];
            stringBuilder.Append(CultureInfo.InvariantCulture,
                $"<mstrans:dictionary translation='{EscapeXml(translation)}'>{EscapeXml(phrase)}</mstrans:dictionary>");

            currentPos = hit.End; // Update current position
        }

        if (currentPos < text.Length)
            stringBuilder.Append(text.AsSpan(currentPos)); // Append text after last match

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Escapes XML special characters in the given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.IndexOfAny(['&', '<', '>', '\'', '"']) < 0)
            return text; // Return original text if there are no special characters

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;")
            .Replace("\"", "&quot;");
    }
}