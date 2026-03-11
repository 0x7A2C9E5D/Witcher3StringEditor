namespace Witcher3StringEditor.Dictionary;

public interface IDictionaryService
{
    /// <summary>
    ///    Dictionary manager service
    /// </summary>
    IDictionaryMangerService DictionaryMangerService { get; }
    
    /// <summary>
    ///    Dynamic dictionary service
    /// </summary>
    IDynamicDictionaryService DynamicDictionaryService { get; }
}