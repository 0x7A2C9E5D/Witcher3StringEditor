namespace Witcher3StringEditor.Dictionary;

public interface IDynamicDictionaryService
{
    bool IsReady { get; }

    DictionaryInfo? CurrentDictionary { get; }

    bool Bind(DictionaryInfo dictionary);

    string Replace(string text);
}