namespace Witcher3StringEditor.Dictionary.Providers;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
public interface IDictionaryProvider
{
    /// <summary>
    ///     Gets the dictionary information for the specified file path.
    /// </summary>
    Task<DictionaryInfo> GetDictionaryInfo(string filePath);

    /// <summary>
    ///     Gets the entries for the specified dictionary.
    /// </summary>
    Task<Dictionary<string, string>> GetEntries(DictionaryInfo dictionary);
}