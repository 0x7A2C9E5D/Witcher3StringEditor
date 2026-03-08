namespace Witcher3StringEditor.Xliff;

/// <summary>
///     Interface for XliffReader
/// </summary>
public interface IXliffReader
{
    /// <summary>
    ///    Reads XliffInfo
    /// </summary>
    /// <param name="path"></param>
    /// <returns>
    ///    XliffInfo
    /// </returns>
    public XliffInfo? ReadInfo(string path);
    
    /// <summary>
    ///    Reads XliffDocument
    /// </summary>
    /// <param name="path"></param>
    /// <returns>
    ///    XliffDocument
    /// </returns>
    public XliffDocument? ReadDocument(string path);

    /// <summary>
    ///    Reads XliffDocument
    /// </summary>
    /// <param name="info"></param>
    /// <returns>
    ///    XliffDocument
    /// </returns>
    public XliffDocument ReadDocument(XliffInfo info);
}