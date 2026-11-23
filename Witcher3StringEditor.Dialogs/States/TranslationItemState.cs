namespace Witcher3StringEditor.Dialogs.States;

/// <summary>
///     The translation state of one item shown in the single-item translation view.
/// </summary>
internal enum TranslationItemState
{
    /// <summary>
    ///     The item has no translation text.
    /// </summary>
    Empty,

    /// <summary>
    ///     The item is being translated.
    /// </summary>
    Translating,

    /// <summary>
    ///     The item has translation text that was not written back to the document.
    /// </summary>
    Translated,

    /// <summary>
    ///     The translation was written back to the document.
    /// </summary>
    Saved,

    /// <summary>
    ///     Translating the item failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     Translating the item was canceled.
    /// </summary>
    Cancelled
}