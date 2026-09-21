namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A service that provides dynamic dictionary functionality
/// </summary>
/// <remarks>
///     Implementations must be safe for concurrent use: <see cref="Bind" /> may be invoked while
///     <see cref="Replace" /> is running (for example when the user switches dictionaries during a batch
///     translation), so the bound state has to be published atomically
/// </remarks>
public interface IDynamicDictionaryReplacer
{
    /// <summary>
    ///     Indicates whether the service is ready to use
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    ///     The currently selected dictionary, or null when no dictionary is bound
    /// </summary>
    DictionaryInfo? CurrentDictionary { get; }

    /// <summary>
    ///     Binds the service to the specified dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary to bind to; must not be null</param>
    /// <param name="cancellationToken">A token used to abort the bind operation</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    /// <remarks>
    ///     Failures do not throw: the cause is logged, <see cref="IsReady" /> becomes <c>false</c> and
    ///     <see cref="CurrentDictionary" /> becomes null, which is the only failure signal for the caller.
    ///     Binding a dictionary that contains no usable entries leaves the service empty but ready
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is null</exception>
    Task Bind(DictionaryInfo dictionary, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Replaces the terms of the bound dictionary inside the specified text
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>
    ///     The text with every matched term wrapped in the markup the translation service understands. When the
    ///     service is not ready, no dictionary is bound, or no term matches, the original text is returned
    ///     unchanged. Matching is case-insensitive and works on whole words and substrings, preferring the
    ///     longest match at the leftmost position
    /// </returns>
    string Replace(string text);
}