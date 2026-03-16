namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A service that provides dynamic dictionary functionality.
/// </summary>
public interface IDynamicDictionaryReplacer
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
    Task<bool> Bind(DictionaryInfo dictionary);

    /// <summary>
    ///     Replaces the specified text with the corresponding translation.
    /// </summary>
    string Replace(string text);
}