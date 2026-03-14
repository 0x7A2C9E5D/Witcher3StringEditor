namespace Witcher3StringEditor.Dictionary.Services;

public class DictionaryService(
    IDictionaryProvider provider,
    IDictionaryMangerService dictionaryMangerService,
    IDynamicDictionaryService dynamicDictionaryService)
    : IDictionaryService
{
    public IDictionaryProvider Provider { get; } = provider;
    
    public IDictionaryMangerService DictionaryMangerService { get; } = dictionaryMangerService;
    
    public IDynamicDictionaryService DynamicDictionaryService { get; } = dynamicDictionaryService;
}