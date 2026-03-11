using System.Globalization;

namespace Witcher3StringEditor.Dictionary;

public interface IDictionaryMangerService
{
    DictionaryInfo? CurrentDictionary { get; set; }

    bool Import(string filePath);

    void Remove(DictionaryInfo dictionary);

    IEnumerable<DictionaryInfo> Find(CultureInfo? language);
}