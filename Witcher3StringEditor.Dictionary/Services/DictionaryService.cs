namespace Witcher3StringEditor.Dictionary.Services;

public class DictionaryService(
    IDictionaryMangerService dictionaryMangerService,
    IDynamicDictionaryService dynamicDictionaryService)
    : IDictionaryService
{
    public IDictionaryMangerService DictionaryMangerService { get; } = dictionaryMangerService;
    public IDynamicDictionaryService DynamicDictionaryService { get; } = dynamicDictionaryService;
}