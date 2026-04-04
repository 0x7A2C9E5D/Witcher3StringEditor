using System.Globalization;
using Witcher3StringEditor.Dictionary;
using Witcher3StringEditor.Dictionary.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     A service that provides dictionary information and entries.
/// </summary>
/// <param name="provider"></param>
/// <param name="dictionaryManager"></param>
/// <param name="dynamicDictionaryReplacer"></param>
public class DictionaryService(
    IDictionaryProvider provider,
    IDictionaryManager dictionaryManager,
    IDynamicDictionaryReplacer dynamicDictionaryReplacer)
    : IDictionaryService
{
    public Task<DictionaryInfo> GetDictionaryInfo(string filePath)
    {
        return provider.GetDictionaryInfo(filePath);
    }

    public Task<Dictionary<string, string>> GetEntries(DictionaryInfo dictionary)
    {
        return provider.GetEntries(dictionary);
    }

    public Task<DictionaryInfo?> Import(string filePath)
    {
        return dictionaryManager.Import(filePath);
    }

    public void Remove(DictionaryInfo dictionary)
    {
        dictionaryManager.Remove(dictionary);
    }

    public IEnumerable<DictionaryInfo> Find(CultureInfo? language)
    {
        return dictionaryManager.Find(language);
    }

    public bool IsReady => dynamicDictionaryReplacer.IsReady;

    public DictionaryInfo? CurrentDictionary => dynamicDictionaryReplacer.CurrentDictionary;

    public async Task Bind(DictionaryInfo dictionary)
    {
        await dynamicDictionaryReplacer.Bind(dictionary);
    }

    public string Replace(string text)
    {
        return dynamicDictionaryReplacer.Replace(text);
    }
}