namespace Witcher3StringEditor.Dictionary.Services;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
public interface IDynamicDictionaryService
{
    /// <summary>
    ///     Indicates whether the service is ready to use.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    ///     The currently selected dictionary.
    /// </summary>
    DictionaryInfo? CurrentDictionary { get; }

    /// <summary>
    ///     Binds the service to the specified dictionary.
    /// </summary>
    bool Bind(DictionaryInfo dictionary);

    /// <summary>
    ///     Replaces the specified text with the corresponding translation.
    /// </summary>
    string Replace(string text);
}