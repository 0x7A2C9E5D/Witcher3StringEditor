using System.Globalization;

namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A class that represents a dictionary info.
/// </summary>
public interface IDictionaryManager
{
    /// <summary>
    ///     Imports a dictionary from a file.
    /// </summary>
    Task<DictionaryInfo?> Import(string filePath);

    /// <summary>
    ///     Removes the specified dictionary.
    /// </summary>
    void Remove(DictionaryInfo dictionary);

    /// <summary>
    ///     Finds all dictionaries matching the specified language.
    /// </summary>
    IEnumerable<DictionaryInfo> Find(CultureInfo? language);
}