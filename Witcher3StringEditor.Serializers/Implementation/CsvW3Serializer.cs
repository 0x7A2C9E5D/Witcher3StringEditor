using System.Collections.Concurrent;
using CommunityToolkit.Diagnostics;
using Cysharp.Text;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Serializers.Abstractions;
using Witcher3StringEditor.Serializers.Internal;

namespace Witcher3StringEditor.Serializers.Implementation;

/// <summary>
///     Provides CSV serialization functionality for The Witcher 3 string items
///     Implements the ICsvW3Serializer interface to handle reading from and writing to CSV files
/// </summary>
public class CsvW3Serializer(IBackupService backupService) : ICsvW3Serializer
{
    /// <summary>
    ///     Deserializes The Witcher 3 string items from a CSV file
    /// </summary>
    /// <param name="filePath">The path to the CSV file to deserialize</param>
    /// <returns>
    ///     A task that represents the asynchronous deserialize operation.
    ///     The task result contains the deserialized The Witcher 3 string items, or an empty list if an error occurred
    /// </returns>
    public async Task<IReadOnlyList<IW3StringItem>> Deserialize(string filePath)
    {
        try
        {
            var w3StringItems = new ConcurrentBag<IW3StringItem>();
            var lines = await File.ReadAllLinesAsync(filePath);
            await Parallel.ForEachAsync(lines, (line, _) =>
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
                    return ValueTask.CompletedTask;
                var parts = line.Split('|');
                if (parts.Length != 4) return ValueTask.CompletedTask;

                w3StringItems.Add(new W3StringStringItem
                {
                    StrId = parts[0].Trim(),
                    KeyHex = parts[1].Trim(),
                    KeyName = parts[2].Trim(),
                    Text = parts[3].Trim()
                });

                return ValueTask.CompletedTask;
            });

            return w3StringItems.ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deserializing the CSV file: {Path}.", filePath); // Log errors
            return []; // Return empty list on error
        }
    }

    /// <summary>
    ///     Serializes The Witcher 3 string items to a CSV file
    /// </summary>
    /// <param name="w3StringItems">The Witcher 3 string items to serialize</param>
    /// <param name="context">The serialization context containing output directory and target language information</param>
    /// <returns>
    ///     A task that represents the asynchronous serialize operation.
    ///     The task result indicates whether the serialization was successful
    /// </returns>
    public async Task<bool> Serialize(IReadOnlyList<IW3StringItem> w3StringItems, W3SerializationContext context)
    {
        try
        {
            var languageName =
                Enum.GetName(context.TargetLanguage)!.ToLowerInvariant(); // Lowercase language name for filename
            var csvLanguageIdentifier = context.TargetLanguage switch // Get language ID for CSV metadata
            {
                W3Language.Ar or W3Language.Br or W3Language.Cn or W3Language.Esmx or W3Language.Kr or W3Language.Tr
                    => "cleartext", // Special case: use "cleartext" for these languages
                _ => languageName // Default: use language name
            };
            await WriteFileWithBackup(Path.Combine(context.OutputDirectory, $"{languageName}.csv"),
                BuildCsvContent(w3StringItems, csvLanguageIdentifier)); // Write CSV with backup
            return true; // Return success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while serializing the CSV file."); // Log serialization errors
            return false; // Return failure
        }
    }

    /// <summary>
    ///     Writes content to a file with backup creation if the file already exists
    /// </summary>
    /// <param name="filePath">The path to the file to write</param>
    /// <param name="content">The content to write to the file</param>
    /// <returns>A task that represents the asynchronous write operation</returns>
    private async Task WriteFileWithBackup(string filePath, string content)
    {
        if (File.Exists(filePath)) // If file exists
            Guard.IsTrue(await backupService.Backup(filePath)); // Create backup
        await File.WriteAllTextAsync(filePath, content); // Write file
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
            stringBuilder.AppendLine(
                $"{w3StringItem.StrId}|{w3StringItem.KeyHex}|{w3StringItem.KeyName}|{w3StringItem.Text}"); // CSV row format: StrId|KeyHex|KeyName|Text
        return stringBuilder.ToString(); // Return complete CSV content
    }
}