using System.Globalization;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///    A class representing a group of dictionaries
/// </summary>
public class DictionaryGroup
{
    /// <summary>
    ///    Initializes a new instance of the DictionaryGroup class
    /// </summary>
    public CultureInfo TargetLanguage { get; set; }
    
    /// <summary>
    ///    Initializes a new instance of the DictionaryGroup class
    /// </summary>
    public List<string> DictionaryNames { get; set; }
}