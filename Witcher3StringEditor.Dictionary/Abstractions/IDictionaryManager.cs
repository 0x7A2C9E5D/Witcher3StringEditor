using System.Globalization;

namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A class that represents a dictionary info.
/// </summary>
public interface IDictionaryManager
{
    /// <summary>
    ///     Imports a dictionary from a file, replacing any dictionary stored under the same file name.
    /// </summary>
    Task<DictionaryInfo?> Import(string filePath);

    /// <summary>
    ///     Determines whether a dictionary with the same file name is already registered.
    /// </summary>
    /// <param name="filePath">The path of the dictionary file being imported.</param>
    /// <returns><c>true</c> when importing would overwrite an existing dictionary.</returns>
    bool ContainsDuplicate(string filePath);

    /// <summary>
    ///     Removes the specified dictionary.
    /// </summary>
    void Remove(DictionaryInfo dictionary);

    /// <summary>
    ///     Finds all dictionaries matching the specified language.
    /// </summary>
    IEnumerable<DictionaryInfo> Find(CultureInfo? language);
}