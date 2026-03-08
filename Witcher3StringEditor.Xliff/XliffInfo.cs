using System.Globalization;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff;

/// <summary>
///     XliffInfo
/// </summary>
[PublicAPI]
public record XliffInfo
{
    /// <summary>
    ///     File path
    /// </summary>
    public required string FilePath { get; set; }
    
    /// <summary>
    ///     Xliff version
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    ///     Source language
    /// </summary>  
    public required CultureInfo SourceLanguage { get; init; }

    /// <summary>
    ///     Target language
    /// </summary>
    public required CultureInfo TargetLanguage { get; init; }
    
    /// <summary>
    ///     Count of translation units
    /// </summary>
    public int TermCount { get; set; }
}