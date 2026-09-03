namespace Witcher3StringEditor.Messaging;

/// <summary>
///     Provides a centralized collection of message tokens used for communication between components
///     These tokens are used with the messaging system to identify specific types of messages
/// </summary>
public static class MessageTokens
{
    /// <summary>
    ///     Token for messages indicating that the translator is currently busy
    /// </summary>
    public const string TranslatorIsBusy = "TranslatorIsBusy";

    /// <summary>
    ///     Token for messages indicating that a recent file has been opened
    /// </summary>
    public const string RecentFileOpened = "RecentFileOpened";

    /// <summary>
    ///     Token for messages indicating that the W3Strings path has changed
    /// </summary>
    public const string W3StringsPathChanged = "W3StringsPathChanged";

    /// <summary>
    ///     Token for messages indicating that the game executable path has changed
    /// </summary>
    public const string GameExePathChanged = "GameExePathChanged";

    /// <summary>
    ///     Token for messages indicating that the page size has changed
    /// </summary>
    public const string PageSizeChanged = "PageSizeChanged";

    /// <summary>
    ///     Token for messages indicating that the translator has changed
    /// </summary>
    public const string TranslatorChanged = "TranslatorChanged";

    /// <summary>
    ///     Token for messages indicating that the data grid paged source has changed
    /// </summary>
    public const string DataGridPagedSourceChanged = "DataGridPagedSourceChanged";

    /// <summary>
    ///     Token for messages indicating that a request for data grid paged source has been made
    /// </summary>
    public const string RequestDataGridPagedSource = "RequestDataGridPagedSource";

    /// <summary>
    ///     Token for messages indicating that a recent file entry has been changed
    /// </summary>
    public const string RecentFileEntry = "RecentFileEntry";
}