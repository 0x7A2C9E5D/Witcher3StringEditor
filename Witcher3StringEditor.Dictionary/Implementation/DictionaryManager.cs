using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Serilog;
using Witcher3StringEditor.Dictionary.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Dictionary.Implementation;

/// <summary>
///     Dictionary manager
/// </summary>
public class DictionaryManager : IDictionaryManager
{
    private readonly ICultureMatcher cultureMatcher; // Culture matcher

    private readonly List<DictionaryInfo> dictionaries = []; // Dictionaries

    private readonly IDictionaryProvider dictionaryProvider; // Dictionary provider

    /// <summary>
    ///     Dictionary service
    /// </summary>
    /// <param name="matcher"></param>
    /// <param name="provider"></param>
    public DictionaryManager(ICultureMatcher matcher, IDictionaryProvider provider)
    {
        cultureMatcher = matcher; // Culture matcher
        dictionaryProvider = provider; // Dictionary provider
        Directory.CreateDirectory(AppPaths.DictionaryDirectory); // Create dictionary directory if it doesn't exist
        LoadDictionariesFromDirectory(AppPaths.DictionaryDirectory); // Load dictionaries from directory
    }

    /// <summary>
    ///     Import dictionary
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public async Task<DictionaryInfo?> Import(string filePath)
    {
        if (!await HandleDuplicateDictionary(filePath))
            return null;

        return await CopyAndRegisterDictionary(filePath);
    }

    /// <summary>
    ///     Remove dictionary
    /// </summary>
    /// <param name="dictionary"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Remove(DictionaryInfo dictionary)
    {
        if (!dictionaries.Contains(dictionary)) return; // Not found
        File.Delete(dictionary.Path); // Delete the file
        dictionaries.Remove(dictionary); // Remove from collection
        Log.Information("Removed dictionary: {Path}", dictionary.Path);
    }

    /// <summary>
    ///     Find dictionaries
    /// </summary>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<DictionaryInfo> Find(CultureInfo? language)
    {
        if (language == null) return dictionaries; // If no language specified, return all dictionaries

        // Get all target languages and find matches using culture matcher
        var languages = dictionaries.Select(x => x.TargetLanguage).ToList();
        var matchedLanguages = cultureMatcher.Matches(language, languages).ToHashSet();

        // Return dictionaries with matched target languages
        return dictionaries.Where(d => matchedLanguages.Contains(d.TargetLanguage));
    }

    /// <summary>
    ///     Handle duplicate dictionary
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<bool> HandleDuplicateDictionary(string filePath)
    {
        // Check for duplicate
        var fileName = Path.GetFileName(filePath);
        var found =
            dictionaries.Where(x => Path.GetFileName(x.Path) == fileName).ToList();
        if (found.Count == 0) return true;

        // Ask for overwrite
        if (!await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.DictionaryOverwriteConfirm)) return false;
        found.ForEach(x => dictionaries.Remove(x));
        return true;
    }

    /// <summary>
    ///     Copies the dictionary file and registers it in the collection.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <returns>The newly imported dictionary info.</returns>
    private async Task<DictionaryInfo> CopyAndRegisterDictionary(string filePath)
    {
        // Copy the dictionary file
        var fileName = Path.GetFileName(filePath);
        var dictionaryInfo = await dictionaryProvider.GetDictionaryInfo(filePath);
        var destFileName = Path.Combine(AppPaths.DictionaryDirectory, fileName);
        File.Copy(filePath, destFileName, true);

        // Register the dictionary
        var newDictionaryInfo = dictionaryInfo with { Path = destFileName };
        dictionaries.Add(newDictionaryInfo);

        Log.Information("Imported dictionary: {Path}", destFileName);
        return newDictionaryInfo;
    }

    /// <summary>
    ///     Load dictionaries from directory
    /// </summary>
    /// <param name="path"></param>
    private void LoadDictionariesFromDirectory(string path)
    {
        Task.Run(async () =>
        {
            var dictionaryFiles = Directory.GetFiles(path)
                .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).ToArray(); // Get dictionary files
            foreach (var dictionaryFile in dictionaryFiles)
                try
                {
                    var dictionaryInfo =
                        await dictionaryProvider.GetDictionaryInfo(dictionaryFile); // Get dictionary info
                    dictionaries.Add(dictionaryInfo); // Add to collection
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to load dictionary file: {Path}",
                        dictionaryFile); // Log error if failed to load dictionary file
                }

            Log.Information("Loaded {Count} dictionary files", dictionaryFiles.Length);
        });
    }
}