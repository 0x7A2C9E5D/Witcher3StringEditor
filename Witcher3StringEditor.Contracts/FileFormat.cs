namespace Witcher3StringEditor.Contracts;

/// <summary>
///     The file formats The Witcher 3 string items can be read from and written to
///     One of them is the game's own container; the other two are interchange formats used to
///     edit and translate the items outside the game
/// </summary>
public enum FileFormat
{
    /// <summary>
    ///     CSV (comma-separated values) text file
    ///     A plain text format where values are separated by commas
    /// </summary>
    Csv = 0,

    /// <summary>
    ///     W3Strings container
    ///     The native binary format used by The Witcher 3 for storing string resources
    /// </summary>
    W3Strings = 1,

    /// <summary>
    ///     Excel workbook
    ///     A spreadsheet format commonly used for data management and analysis
    /// </summary>
    Excel = 2
}