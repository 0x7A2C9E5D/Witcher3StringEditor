namespace Witcher3StringEditor.Dictionary;

public interface IDictionaryProvider
{
    DictionaryInfo GetDictionaryInfo(string filePath);

    Dictionary<string, string> GetEntries(DictionaryInfo dictionary);
}