using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MoreLinq;
using Serilog;
using Witcher3StringEditor.Dictionary.Providers;
using Witcher3StringEditor.Helpers;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dictionary.Services;

public class DictionaryMangerService : IDictionaryMangerService
{
    private readonly ICultureMatcher cultureMatcher; // Culture matcher

    private readonly List<DictionaryInfo> dictionaries = []; // Dictionaries

    private readonly IDictionaryProvider dictionaryProvider; // Dictionary provider

    /// <summary>
    ///     Dictionary service
    /// </summary>
    /// <param name="matcher"></param>
    /// <param name="provider"></param>
    public DictionaryMangerService(ICultureMatcher matcher, IDictionaryProvider provider)
    {
        cultureMatcher = matcher; // Culture matcher
        dictionaryProvider = provider; // Dictionary provider
        if (!Path.Exists(PathHelper.DictionaryDirectory)) 
            Directory.CreateDirectory(PathHelper.DictionaryDirectory); // Create dictionary directory if it doesn't exist
        LoadDictionariesFromDirectory(PathHelper.DictionaryDirectory); // Load dictionaries from directory
    }

    public DictionaryInfo? CurrentDictionary { get; set; } // Current dictionary

    /// <summary>
    ///     Import dictionary
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public async Task<DictionaryInfo?> Import(string filePath)
    {
        var fileName = Path.GetFileName(filePath); // Get file name
        var found = dictionaries
            .Where(x => Path.GetFileName(x.Path) == fileName).ToList(); // Find dictionaries with the same file name
        if (found.Count != 0) // If found, ask user if they want to overwrite
        {
            if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                    MessageTokens.DictionaryOverwriteConfirm)) // Ask user if they want to overwrite
                found.ForEach(x => dictionaries.Remove(x)); // Remove found dictionaries
            else
                return null; // User canceled, return null
        }

        var dictionaryInfo = dictionaryProvider.GetDictionaryInfo(filePath); // Get dictionary info
        var destFileName = Path.Combine(PathHelper.DictionaryDirectory, fileName); // Get destination file name
        File.Copy(filePath, destFileName, true); // Copy file
        var newDictionaryInfo = dictionaryInfo with { Path = destFileName }; // Create new dictionary info
        dictionaries.Add(newDictionaryInfo); // Add to collection

        return newDictionaryInfo; // Return new dictionary info
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
    ///     Load dictionaries from directory
    /// </summary>
    /// <param name="path"></param>
    private void LoadDictionariesFromDirectory(string path)
    {
        var dictionaryFiles = Directory.GetFiles(path)
            .Where(f => f.EndsWith(".txt")); // Get dictionary files
        dictionaryFiles.ForEach(dictionaryFile =>
        {
            try
            {
                var dictionaryInfo = dictionaryProvider.GetDictionaryInfo(dictionaryFile); // Get dictionary info
                dictionaries.Add(dictionaryInfo); // Add to collection
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load dictionary file: {Path}", dictionaryFile); // Log error if failed to load dictionary file
            }
        });
    }
}