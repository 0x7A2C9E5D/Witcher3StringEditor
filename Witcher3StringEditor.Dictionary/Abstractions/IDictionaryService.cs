namespace Witcher3StringEditor.Dictionary.Abstractions;

/// <summary>
///     A composite service that aggregates the dictionary capabilities: reading dictionary information and
///     entries (<see cref="IDictionaryProvider" />), importing, removing and finding dictionaries
///     (<see cref="IDictionaryManager" />), and binding a dictionary for dynamic replacement
///     (<see cref="IDynamicDictionaryReplacer" />)
/// </summary>
/// <remarks>
///     Consumers that only need dynamic replacement should depend on <see cref="IDynamicDictionaryReplacer" />
///     directly, so they are not coupled to the import/remove/query surface they never use
/// </remarks>
public interface IDictionaryService : IDictionaryProvider, IDictionaryManager, IDynamicDictionaryReplacer;