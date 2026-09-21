namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A service that provides dictionary information and entries
/// </summary>
/// <remarks>
///     Implementations throw <see cref="InvalidDataException" /> when the file exists but is not a valid
///     dictionary, and let the <see cref="System.IO" /> exceptions of the underlying file access surface
///     unchanged when the file cannot be read at all
/// </remarks>
public interface IDictionaryProvider
{
    /// <summary>
    ///     Gets the dictionary information for the specified file path
    /// </summary>
    /// <param name="filePath">The absolute path of the dictionary file</param>
    /// <returns>The dictionary information of the specified file</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    /// <exception cref="InvalidDataException">The file is empty or not a valid dictionary</exception>
    /// <exception cref="IOException">The file could not be read</exception>
    Task<DictionaryInfo> GetDictionaryInfo(string filePath);

    /// <summary>
    ///     Gets the entries for the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary to read the entries from</param>
    /// <returns>
    ///     The entries of the dictionary, keyed case-insensitively (ordinal). A duplicated key keeps the last
    ///     value found in the file; the enumeration order is unspecified
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is null</exception>
    /// <exception cref="IOException">The dictionary file could not be read</exception>
    Task<Dictionary<string, string>> GetEntries(DictionaryInfo dictionary);
}