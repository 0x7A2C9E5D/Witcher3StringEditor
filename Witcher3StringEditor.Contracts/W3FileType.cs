namespace Witcher3StringEditor.Contracts;

/// <summary>
///     Represents the supported file types for The Witcher 3 string operations
///     This enumeration defines the different file formats that can be used to serialize or deserialize The Witcher 3
///     string items
/// </summary>
public enum W3FileType
{
    /// <summary>
    ///     CSV (Comma-Separated Values) file format
    ///     A plain text format where values are separated by commas
    /// </summary>
    Csv = 0,

    /// <summary>
    ///     W3Strings file format
    ///     The native binary format used by The Witcher 3 for storing string resources
    /// </summary>
    W3Strings = 1,

    /// <summary>
    ///     Excel file format
    ///     A spreadsheet format commonly used for data management and analysis
    /// </summary>
    Excel = 2
}