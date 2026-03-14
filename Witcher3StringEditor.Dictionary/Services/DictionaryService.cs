namespace Witcher3StringEditor.Dictionary.Services;

/// <summary>
///     Dictionary service
/// </summary>
/// <param name="provider"></param>
/// <param name="dictionaryMangerService"></param>
/// <param name="dynamicDictionaryService"></param>
public class DictionaryService(
    IDictionaryProvider provider,
    IDictionaryMangerService dictionaryMangerService,
    IDynamicDictionaryService dynamicDictionaryService)
    : IDictionaryService
{
    /// <summary>
    ///     Dictionary manger service
    /// </summary>
    public IDictionaryProvider DictionaryProvider { get; } = provider;

    /// <summary>
    ///     Dictionary manger service
    /// </summary>
    public IDictionaryMangerService DictionaryMangerService { get; } = dictionaryMangerService;

    /// <summary>
    ///     Dynamic dictionary service
    /// </summary>
    public IDynamicDictionaryService DynamicDictionaryService { get; } = dynamicDictionaryService;
}