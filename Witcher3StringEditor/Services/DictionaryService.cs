using System.Globalization;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Dictionary.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
/// <param name="provider">The dictionary provider used to read dictionary content</param>
/// <param name="dictionaryManager">The dictionary manager used to import, remove and find dictionaries</param>
/// <param name="dynamicDictionaryReplacer">The replacer used to bind a dictionary and replace terms</param>
public class DictionaryService(
    IDictionaryProvider provider,
    IDictionaryManager dictionaryManager,
    IDynamicDictionaryReplacer dynamicDictionaryReplacer)
    : IDictionaryService
{
    /// <summary>
    ///     Gets the dictionary information for the specified file
    /// </summary>
    /// <param name="filePath">The path of the dictionary file</param>
    /// <returns>The dictionary information</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    public Task<DictionaryInfo> GetDictionaryInfo(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return provider.GetDictionaryInfo(filePath);
    }

    /// <summary>
    ///     Gets the entries of the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary to read</param>
    /// <returns>The entries of the dictionary</returns>
    public Task<Dictionary<string, string>> GetEntries(DictionaryInfo dictionary)
    {
        return provider.GetEntries(dictionary);
    }

    /// <summary>
    ///     Imports the dictionary stored in the specified file
    /// </summary>
    /// <param name="filePath">The path of the dictionary file to import</param>
    /// <returns>The imported dictionary information, or null when the file could not be imported</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    public Task<DictionaryInfo?> Import(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return dictionaryManager.Import(filePath);
    }

    /// <summary>
    ///     Removes the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary to remove</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is null</exception>
    public void Remove(DictionaryInfo dictionary)
    {
        dictionaryManager.Remove(dictionary);
    }

    /// <summary>
    ///     Determines whether the specified dictionary file is already registered
    /// </summary>
    /// <param name="filePath">The path of the dictionary file</param>
    /// <returns>True when the dictionary is already registered; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    public bool ContainsDuplicate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return dictionaryManager.ContainsDuplicate(filePath);
    }

    /// <summary>
    ///     Finds the registered dictionaries for the specified language
    /// </summary>
    /// <param name="language">The language to filter by, or null for all languages</param>
    /// <returns>The registered dictionaries matching the language</returns>
    public IEnumerable<DictionaryInfo> Find(CultureInfo? language)
    {
        return dictionaryManager.Find(language);
    }

    /// <summary>
    ///     Gets a value indicating whether a dictionary is currently bound
    /// </summary>
    public bool IsReady => dynamicDictionaryReplacer.IsReady;

    /// <summary>
    ///     Gets the currently bound dictionary
    /// </summary>
    public DictionaryInfo? CurrentDictionary => dynamicDictionaryReplacer.CurrentDictionary;

    /// <summary>
    ///     Binds the specified dictionary so its entries are used by <see cref="Replace" />
    /// </summary>
    /// <param name="dictionary">The dictionary to bind</param>
    /// <param name="cancellationToken">A token used to abort the bind operation</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Bind(DictionaryInfo dictionary, CancellationToken cancellationToken = default)
    {
        return dynamicDictionaryReplacer.Bind(dictionary, cancellationToken);
    }

    /// <summary>
    ///     Replaces the terms of the bound dictionary inside the specified text
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>The text with the bound dictionary terms replaced</returns>
    public string Replace(string text)
    {
        return dynamicDictionaryReplacer.Replace(text);
    }
}