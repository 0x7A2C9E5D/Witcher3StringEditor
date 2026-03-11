using System.Globalization;

namespace Witcher3StringEditor.Dictionary;

public interface IDictionaryMangerService
{
    DictionaryInfo? CurrentDictionary { get; set; }

    DictionaryInfo? Import(string filePath);

    void Remove(DictionaryInfo dictionary);

    IEnumerable<DictionaryInfo> Find(CultureInfo? language);
}