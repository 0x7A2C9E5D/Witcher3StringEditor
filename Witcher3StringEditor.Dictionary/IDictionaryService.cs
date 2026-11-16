namespace Witcher3StringEditor.Dictionary;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
public interface IDictionaryService : IDictionaryProvider, IDictionaryManager, IDynamicDictionaryReplacer;