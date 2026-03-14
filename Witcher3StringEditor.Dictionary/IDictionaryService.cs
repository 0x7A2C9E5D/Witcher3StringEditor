namespace Witcher3StringEditor.Dictionary;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
public interface IDictionaryService
{
    /// <summary>
    ///     Dictionary manager service
    /// </summary>
    IDictionaryMangerService DictionaryMangerService { get; }

    /// <summary>
    ///     Dynamic dictionary service
    /// </summary>
    IDynamicDictionaryService DynamicDictionaryService { get; }
}