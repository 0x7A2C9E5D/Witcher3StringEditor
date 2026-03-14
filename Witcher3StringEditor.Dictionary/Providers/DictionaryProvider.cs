using System.Globalization;

namespace Witcher3StringEditor.Dictionary.Providers;

/// <summary>
///     A dictionary provider for custom format dictionary files
///     File format:
///     Line 1: ;Dictionary Name|SourceLang|TargetLang
///     Line 2+: ;Note|... or KEY|VALUE
/// </summary>
public class DictionaryProvider : IDictionaryProvider
{
    private const string CommentPrefix = ";";
    private const char Separator = '|';
    private const string NotePrefix = "Note";

    public DictionaryInfo GetDictionaryInfo(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        
        if (lines.Length == 0)
            throw new InvalidDataException($"Dictionary file is empty: {filePath}");

        // Parse header
        var header = ParseHeader(lines[0].Trim());
        
        // Extract note and count entries
        var (note, termCount) = ExtractMetadata(lines.Skip(1));

        return new DictionaryInfo(
            filePath,
            header.name,
            note,
            CultureInfo.GetCultureInfo(header.sourceLang),
            CultureInfo.GetCultureInfo(header.targetLang),
            termCount
        );
    }

    public Dictionary<string, string> GetEntries(DictionaryInfo dictionary)
    {
        var lines = File.ReadAllLines(dictionary.Path);
        return ParseEntries(lines);
    }

    /// <summary>
    ///     Parses and validates the header line using pattern matching
    /// </summary>
    private static (string name, string sourceLang, string targetLang) ParseHeader(string headerLine)
    {
        if (!headerLine.StartsWith(CommentPrefix))
            throw new InvalidDataException($"Missing header (must start with '{CommentPrefix}'");

        var parts = headerLine.Split(Separator, 3);

        if (parts.Length != 3)
            throw new InvalidDataException(
                $"Invalid header format. Expected: ;Name|SourceLang|TargetLang");

        var name = parts[0].Trim();
        var sourceLang = parts[1].Trim().ToLowerInvariant();
        var targetLang = parts[2].Trim().ToLowerInvariant();

        // Validate language codes
        try
        {
            _ = CultureInfo.GetCultureInfo(sourceLang);
            _ = CultureInfo.GetCultureInfo(targetLang);
        }
        catch (CultureNotFoundException)
        {
            throw new InvalidDataException($"Invalid language code '{sourceLang}' or '{targetLang}'");
        }

        return (name, sourceLang, targetLang);
    }

    /// <summary>
    ///     Extracts note and counts valid entries in a single pass
    /// </summary>
    private static (string note, int termCount) ExtractMetadata(IEnumerable<string> lines)
    {
        string? note = null;
        var termCount = 0;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            if (trimmedLine.StartsWith(CommentPrefix))
            {
                // Extract note on first occurrence
                if (note is not null) continue;
                var commentContent = trimmedLine[1..].Trim();
                if (!commentContent.StartsWith($"{NotePrefix}{Separator}", StringComparison.OrdinalIgnoreCase))
                    continue;
                var parts = commentContent.Split(Separator, 2);
                if (parts.Length == 2)
                    note = parts[1].Trim();
            }
            else if (IsValidEntry(trimmedLine))
            {
                termCount++;
            }
        }

        return (note ?? string.Empty, termCount);
    }

    /// <summary>
    ///     Parses dictionary entries from file lines
    /// </summary>
    private static Dictionary<string, string> ParseEntries(IEnumerable<string> lines)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(CommentPrefix))
                continue;

            var parts = trimmedLine.Split(Separator, 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                entries[key] = value;
            }
        }

        return entries;
    }

    /// <summary>
    ///     Validates if a line is a valid dictionary entry
    /// </summary>
    private static bool IsValidEntry(string line)
    {
        if (line.StartsWith(CommentPrefix))
            return false;

        var parts = line.Split(Separator, 2);
        return parts.Length == 2 &&
               !string.IsNullOrWhiteSpace(parts[0].Trim()) &&
               !string.IsNullOrWhiteSpace(parts[1].Trim());
    }
}
