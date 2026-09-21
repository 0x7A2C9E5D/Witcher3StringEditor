using CommunityToolkit.Diagnostics;
using Cysharp.Text;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Provides CSV serialization functionality for The Witcher 3 string items
///     Implements the ICsvW3Serializer interface to handle reading from and writing to CSV files
/// </summary>
public class CsvW3Serializer(IBackupService backupService) : ICsvW3Serializer
{
    /// <summary>
    ///     The field delimiter used by the W3Strings CSV format
    /// </summary>
    private const char FieldDelimiter = '|';

    /// <summary>
    ///     Deserializes The Witcher 3 string items from a CSV file
    /// </summary>
    /// <param name="filePath">The path to the CSV file to deserialize</param>
    /// <param name="cancellationToken">A token used to abort the read</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    /// <exception cref="IOException">The file could not be read</exception>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var fileStream = File.OpenRead(filePath); // Open file stream
        using var reader = new StreamReader(fileStream); // Create stream reader
        var items = new List<IW3StringItem>(); // Create list to store items
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line) // Read lines
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';')) continue; // Skip empty lines and comments
            var parts = line.Split(FieldDelimiter); // Split line into parts
            if (parts.Length != 4)
            {
                Log.Warning("Skipped malformed CSV line {LineNumber} in {Path}", lineNumber, filePath);
                continue;
            }

            items.Add(new W3StringItem // Create new string item
            {
                StrId = parts[0].Trim(), // Extract string ID
                KeyHex = parts[1].Trim(), // Extract key hex
                KeyName = parts[2].Trim(), // Extract key name
                Text = parts[3].Trim() // Extract text
            });
        }

        return items; // Return list of items
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a CSV file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing output directory and target language information</param>
    /// <param name="cancellationToken">A token used to abort to write</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var languageName =
                GetLanguageName(context.TargetLanguage); // Lowercase language name for filename
            var csvLanguageIdentifier = context.TargetLanguage switch // Get language ID for CSV metadata
            {
                W3Language.Ar or W3Language.Br or W3Language.Cn or W3Language.Esmx or W3Language.Kr or W3Language.Tr
                    => "cleartext", // Special case: use "cleartext" for these languages
                _ => languageName // Default: use language name
            };
            await WriteFileWithBackup(Path.Combine(context.OutputDirectory, $"{languageName}.csv"),
                BuildCsvContent(w3StringItems, csvLanguageIdentifier), cancellationToken); // Write CSV with backup
            return true; // Return success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while serializing the CSV file"); // Log serialization errors
            return false; // Return failure
        }
    }

    /// <summary>
    ///     Gets the lower-case language name used in the file name
    /// </summary>
    /// <param name="language">The target language</param>
    /// <returns>The lower-case name of the language</returns>
    /// <exception cref="ArgumentOutOfRangeException">The language value is not defined</exception>
    private static string GetLanguageName(W3Language language)
    {
        return Enum.GetName(language)?.ToLowerInvariant()
               ?? throw new ArgumentOutOfRangeException(nameof(language), language,
                   "The target language is not defined.");
    }

    /// <summary>
    ///     Writes content to a file with backup creation if the file already exists
    ///     The whole read/backup/write sequence is serialized and to write itself is atomic
    /// </summary>
    /// <param name="filePath">The path to the file to write</param>
    /// <param name="content">The content to write to the file</param>
    /// <param name="cancellationToken">A token used to abort to write</param>
    /// <returns>A task that represents the asynchronous write operation</returns>
    private async Task WriteFileWithBackup(string filePath, string content, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath)) // If file exists
            Guard.IsTrue(await backupService.BackupAsync(filePath)); // Create backup
        await File.WriteAllTextAsync(filePath, content, cancellationToken); // Write content to file
    }

    /// <summary>
    ///     Builds the CSV content from a collection of The Witcher 3 string items
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to include in the CSV content</param>
    /// <param name="lang">The language identifier for the CSV metadata</param>
    /// <returns>The complete CSV content as a string</returns>
    private static string BuildCsvContent(IReadOnlyCollection<IW3StringItem> w3StringItems, string lang)
    {
        using var stringBuilder = ZString.CreateStringBuilder(); // Efficient CSV content builder
        stringBuilder.AppendLine($";meta[language={lang}]"); // Language metadata header
        stringBuilder.AppendLine("; id      |key(hex)|key(str)| text"); // CSV column headers
        foreach (var w3StringItem in w3StringItems) // Process each string item
            stringBuilder.AppendLine($"{w3StringItem.StrId}|{w3StringItem.KeyHex}|{w3StringItem.KeyName}|{w3StringItem.Text}"); // Append string item to CSV content
        return stringBuilder.ToString(); // Return complete CSV content
    }
}