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

/// <summary>
///     Tracks the translation state of a single item and reports every actual transition.
/// </summary>
internal sealed class TranslationItemStateMachine
{
    /// <summary>
    ///     Gets the current item state.
    /// </summary>
    public TranslationItemState Current { get; private set; } = TranslationItemState.Empty;

    /// <summary>
    ///     Occurs after the state changed.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    ///     Moves an untranslated, failed or canceled item into the translating state.
    /// </summary>
    public void BeginTranslation()
    {
        if (Current is TranslationItemState.Empty or TranslationItemState.Failed
            or TranslationItemState.Cancelled)
            Set(TranslationItemState.Translating);
    }

    /// <summary>
    ///     Marks a translating item as translated.
    /// </summary>
    public void MarkTranslated()
    {
        TransitionFrom(TranslationItemState.Translating, TranslationItemState.Translated);
    }

    /// <summary>
    ///     Marks a translating item as failed.
    /// </summary>
    public void MarkFailed()
    {
        TransitionFrom(TranslationItemState.Translating, TranslationItemState.Failed);
    }

    /// <summary>
    ///     Marks a translating item as canceled.
    /// </summary>
    public void MarkCancelled()
    {
        TransitionFrom(TranslationItemState.Translating, TranslationItemState.Cancelled);
    }

    /// <summary>
    ///     Marks a translated item as saved.
    /// </summary>
    public void MarkSaved()
    {
        TransitionFrom(TranslationItemState.Translated, TranslationItemState.Saved);
    }

    /// <summary>
    ///     Applies a manual text edit: text makes the item translated again, an empty value clears it.
    /// </summary>
    /// <param name="hasText">Whether the translated text is not empty</param>
    public void TextChanged(bool hasText)
    {
        if (Current == TranslationItemState.Translating) return;
        Set(hasText ? TranslationItemState.Translated : TranslationItemState.Empty);
    }

    private void TransitionFrom(TranslationItemState expected, TranslationItemState next)
    {
        if (Current != expected) return;
        Set(next);
    }

    private void Set(TranslationItemState next)
    {
        if (next == Current) return;
        Current = next;
        StateChanged?.Invoke();
    }
}