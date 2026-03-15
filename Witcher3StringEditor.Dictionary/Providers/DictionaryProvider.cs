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
    public DictionaryInfo GetDictionaryInfo(string filePath)
    {
        var lines = File.ReadAllLines(filePath); // Read all lines from the file

        // Validate file is not empty
        if (lines.Length == 0)
            throw new InvalidDataException($"Dictionary file is empty: {filePath}");

        // Parse header
        var (name, sourceLang, targetLang) = ParseHeader(lines[0].Trim());
        
        if(string.IsNullOrWhiteSpace(name))
            name = Path.GetFileNameWithoutExtension(filePath); // Fallback to file name if name is empty

        // Extract note and count entries
        var (note, termCount) = ExtractMetadata(lines.Skip(1));

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
                "Invalid header format. Expected: ;Name|SourceLang|TargetLang");

        var name = parts[0].Trim();
        var sourceLangRaw = parts[1].Trim();
        var targetLangRaw = parts[2].Trim();

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
            throw new InvalidDataException(
                $"Invalid language code '{sourceLangRaw}' or '{targetLangRaw}'. " +
                $"Supported codes: {string.Join(", ", W3LangMap.Keys)} or standard CultureInfo codes");
        }

        return (name, sourceLang, targetLang);
    }

    /// <summary>
    ///     Normalizes a language code from Witcher 3 format to standard CultureInfo format
    ///     Only converts Witcher 3 specific abbreviations, keeps standard codes as-is
    /// </summary>
    private static string NormalizeLanguageCode(string code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException(@"Language code cannot be empty", nameof(code))
            : W3LangMap.GetValueOrDefault(code, code); // Only convert Witcher 3 specific codes, otherwise keep original
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

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value)) entries[key] = value;
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