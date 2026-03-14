using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MoreLinq;
using Serilog;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dictionary.Services;

public class DictionaryMangerService : IDictionaryMangerService
{
#if DEBUG
    private static bool IsDebug => true; // Debug
#else
    private static bool IsDebug => false; // Release
#endif

    private readonly ICultureMatcher cultureMatcher; // Culture matcher

    private readonly IDictionaryProvider dictionaryProvider; // Dictionary provider

    private readonly string dictionaryPath; // Dictionary path

    public DictionaryInfo? CurrentDictionary { get; set; } // Current dictionary

    private readonly List<DictionaryInfo> dictionaries = []; // Dictionaries

    /// <summary>
    ///     Dictionary service
    /// </summary>
    /// <param name="matcher"></param>
    /// <param name="provider"></param>
    public DictionaryMangerService(ICultureMatcher matcher, IDictionaryProvider provider)
    {
        cultureMatcher = matcher; // Culture matcher
        dictionaryProvider = provider; // Dictionary provider
        dictionaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            , IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor", "Dictionaries"); // Dictionary path
        LoadDictionariesFromDirectory(dictionaryPath); // Load dictionaries from directory
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
                Log.Error(e, "Failed to load dictionary file: {Path}", dictionaryFile);
            }
        });
    }

    /// <summary>
    ///     Import dictionary
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public async Task<DictionaryInfo?> Import(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (dictionaries.Any(x => Path.GetFileName(x.Path) == fileName))
        {
            if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                    MessageTokens.DictionaryOverwriteConfirm))
                dictionaries.RemoveAll(x => x.Path == filePath);
            else
                return null;
        }

        var dictionaryInfo = dictionaryProvider.GetDictionaryInfo(filePath); // Get dictionary info
        var destFileName = Path.Combine(dictionaryPath, Path.GetFileName(filePath)); // Get destination file name
        File.Copy(filePath, destFileName, true); // Copy file
        var newDictionaryInfo = dictionaryInfo with { Path = destFileName }; // Create new dictionary info
        dictionaries.Add(newDictionaryInfo); // Add to collection

        return dictionaryInfo;
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
        if (language == null) return dictionaries;

        // Get all target languages and find matches using culture matcher
        var languages = dictionaries.Select(x => x.TargetLanguage).ToList();
        var matchedLanguages = cultureMatcher.Matches(language, languages).ToHashSet();

        // Return dictionaries with matched target languages
        return dictionaries.Where(d => matchedLanguages.Contains(d.TargetLanguage));
    }
}