using System.Collections.Concurrent;
using System.Globalization;
using Witcher3StringEditor.Dictionary.Abstractions;

namespace Witcher3StringEditor.Dictionary.Implementation;

/// <summary>
///     A dictionary provider for custom format dictionary files
///     File format:
///     Line 1: ;Dictionary Name|SourceLang|TargetLang
///     Line 2+: ;Note|... or KEY|VALUE
/// </summary>
public class DictionaryProvider : IDictionaryProvider
{
    private const string CommentPrefix = ";"; // Comment prefix
    private const char Separator = '|'; // Separator between key and value
    private const string NotePrefix = "Note"; // Note prefix

    /// <summary>
    ///     Maps Witcher 3 special language codes to standard CultureInfo codes
    /// </summary>
    private static readonly Dictionary<string, string> W3LangMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "JP", "ja" },
        { "KR", "ko" },
        { "CN", "zh-Hans" },
        { "ZH", "zh-Hant" },
        { "BR", "pt-BR" },
        { "ESMX", "es-MX" }
    };

    /// <summary>
    ///     A dictionary provider for custom format dictionary files
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public async Task<DictionaryInfo> GetDictionaryInfo(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath); // Read all lines from the file

        // Validate file is not empty
        if (lines.Length == 0)
            throw new InvalidDataException($"Dictionary file is empty: {filePath}");

        // Parse header
        var (name, sourceLang, targetLang) = ParseHeader(lines[0].Trim());

        if (string.IsNullOrWhiteSpace(name))
            name = Path.GetFileNameWithoutExtension(filePath); // Fallback to file name if name is empty

        // Extract note and count entries
        var (note, termCount) = ExtractMetadata(lines.Skip(1));

        // Create and return dictionary info
        return new DictionaryInfo(
            filePath,
            name,
            note,
            CultureInfo.GetCultureInfo(sourceLang),
            CultureInfo.GetCultureInfo(targetLang),
            termCount
        );
    }

    /// <summary>
    ///     Gets dictionary entries from a file
    /// </summary>
    public async Task<Dictionary<string, string>> GetEntries(DictionaryInfo dictionary)
    {
        var lines = await File.ReadAllLinesAsync(dictionary.Path);
        return ParseEntries(lines);
    }

    /// <summary>
    ///     Parses and validates the header line using pattern matching
    /// </summary>
    private static (string name, string sourceLang, string targetLang) ParseHeader(string headerLine)
    {
        if (!headerLine.StartsWith(CommentPrefix, StringComparison.InvariantCulture))
            throw new InvalidDataException(
                $"Missing header (must start with '{CommentPrefix}'"); // Validate header starts with comment prefix

        var parts = headerLine.Split(Separator, 3); // Split into 3 parts: Name, SourceLang, TargetLang

        if (parts.Length != 3)
            throw new InvalidDataException(
                "Invalid header format. Expected: ;Name|SourceLang|TargetLang"); // Validate header has exactly 3 parts

        var name = parts[0][1..].Trim(); // Extract name (remove comment prefix and trim)
        var sourceLangRaw = parts[1].Trim(); // Extract source language code
        var targetLangRaw = parts[2].Trim(); // Extract target language code

        // Convert Witcher 3 special language codes to standard codes
        var sourceLang = NormalizeLanguageCode(sourceLangRaw);
        var targetLang = NormalizeLanguageCode(targetLangRaw);

        // Validate language codes
        try
        {
            _ = CultureInfo.GetCultureInfo(sourceLang);
            _ = CultureInfo.GetCultureInfo(targetLang);
        }
        catch (CultureNotFoundException)
        {
            // Validate that the normalized language codes are valid CultureInfo codes
            throw new InvalidDataException(
                $"Invalid language code '{sourceLangRaw}' or '{targetLangRaw}'. " +
                $"Supported codes: {string.Join(", ", W3LangMap.Keys)} or standard CultureInfo codes");
        }

        return (name, sourceLang, targetLang); // Return parsed header information
    }

    /// <summary>
    ///     Normalizes a language code from Witcher 3 format to standard CultureInfo format
    ///     Only converts Witcher 3 specific abbreviations, keeps standard codes as-is
    /// </summary>
    private static string NormalizeLanguageCode(string code)
    {
        // Only convert Witcher 3 specific codes, otherwise keep original
        return string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException(@"Language code cannot be empty", nameof(code))
            : W3LangMap.GetValueOrDefault(code, code);
    }

    /// <summary>
    ///     Extracts note and counts valid entries in a single pass
    /// </summary>
    private static (string note, int termCount) ExtractMetadata(IEnumerable<string> lines)
    {
        string? note = null; // Note is optional, so it can be null if not found
        var termCount = 0; // Initialize term count

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim(); // Trim line for accurate processing

            if (string.IsNullOrEmpty(trimmedLine))
                continue; // Skip empty lines

            if (trimmedLine.StartsWith(CommentPrefix,
                    StringComparison.InvariantCulture)) // Process comments for note extraction
            {
                if (note is not null) continue; // Note already found, skip further comments
                var commentContent = trimmedLine[1..].Trim(); // Remove comment prefix and trim
                if (!commentContent.StartsWith($"{NotePrefix}{Separator}", StringComparison.OrdinalIgnoreCase))
                    continue; // Not a note comment, skip
                var parts = commentContent.Split(Separator, 2); // Split into "Note" and the actual note content
                if (parts.Length == 2) // Validate note format and extract note content
                    note = parts[1].Trim(); // Set note if valid format
            }
            else if (IsValidEntry(trimmedLine)) // Count valid entries
            {
                termCount++; // Increment term count for each valid entry
            }
        }

        return (note ?? string.Empty, termCount); // Return note (or empty string if not found) and term count
    }

    /// <summary>
    ///     Parses dictionary entries from file lines
    /// </summary>
    private static Dictionary<string, string> ParseEntries(IEnumerable<string> lines)
    {
        var entries = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        lines.AsParallel() // Process lines in parallel
            .Select(line => line.Trim()) // Trim lines
            .Where(trimmedLine =>
                !string.IsNullOrEmpty(trimmedLine) &&
                !trimmedLine.StartsWith(CommentPrefix)) // Skip empty lines and comments
            .Select(trimmedLine => trimmedLine.Split(Separator, 2)) // Split into key and value
            .Where(parts => parts.Length == 2) // Validate format
            .Select(parts => new { Key = parts[0].Trim(), Value = parts[1].Trim() }) // Create anonymous type
            .Where(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value)) // Skip invalid entries
            .ForAll(x => entries[x.Key] = x.Value); // Add valid entries to the dictionary

        return entries.ToDictionary(x => x.Key, x => x.Value,
            StringComparer.OrdinalIgnoreCase); // Convert to regular dictionary
    }

    /// <summary>
    ///     Validates if a line is a valid dictionary entry
    /// </summary>
    private static bool IsValidEntry(string line)
    {
        if (line.StartsWith(CommentPrefix, StringComparison.InvariantCulture))
            return false; // Comments are not valid entries

        var parts = line.Split(Separator, 2); // Split into key and value, only on the first separator 

        // Valid entry must have exactly 2 parts and both key and value must be non-empty after trimming
        return parts.Length == 2 &&
               !string.IsNullOrWhiteSpace(parts[0].Trim()) &&
               !string.IsNullOrWhiteSpace(parts[1]
                   .Trim());
    }
}