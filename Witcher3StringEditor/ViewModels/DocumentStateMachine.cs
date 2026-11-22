namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     The mutually exclusive phase of the document shown in the main window.
/// </summary>
internal enum DocumentPhase
{
    /// <summary>
    ///     No file has been opened yet.
    /// </summary>
    Unloaded,

    /// <summary>
    ///     A file is being read.
    /// </summary>
    Loading,

    /// <summary>
    ///     A file is open and holds at least one item.
    /// </summary>
    Loaded,

    /// <summary>
    ///     A file is open but holds no item.
    /// </summary>
    Empty
}

/// <summary>
///     The complete document session state: the mutually exclusive phase together with the facets that can
///     co-exist with it (unsaved changes and an active search filter).
/// </summary>
internal readonly record struct DocumentState(
    DocumentPhase Phase,
    bool HasUnsavedChanges,
    bool IsFiltering,
    int ItemCount,
    int VisibleItemCount)
{
    /// <summary>
    ///     Gets a value indicating whether the document can be edited, merged or saved.
    /// </summary>
    public bool CanModify => Phase == DocumentPhase.Loaded;

    /// <summary>
    ///     Gets a value indicating whether the translation dialog can be shown.
    ///     An active search that yields no row disables it; otherwise the loaded document decides.
    /// </summary>
    public bool CanTranslate =>
        Phase == DocumentPhase.Loaded && (!IsFiltering || VisibleItemCount > 0);

    /// <summary>
    ///     Gets a value indicating whether the window can close without asking the user first.
    /// </summary>
    public bool CanCloseSilently => !HasUnsavedChanges;
}

/// <summary>
///     Tracks the document session state and raises <see cref="StateChanged" /> only when the state
///     actually changed.
/// </summary>
internal sealed class DocumentStateMachine
{
    private DocumentState stateBeforeLoad;

    /// <summary>
    ///     Gets the current document session state.
    /// </summary>
    public DocumentState Current { get; private set; } = new(DocumentPhase.Unloaded, false, false, 0, 0);

    /// <summary>
    ///     Occurs after the state changed.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    ///     Enters the loading phase, remembering the state to restore when the load fails.
    /// </summary>
    public void BeginLoad()
    {
        if (Current.Phase == DocumentPhase.Loading) return;
        stateBeforeLoad = Current;
        Set(Current with { Phase = DocumentPhase.Loading, IsFiltering = false });
    }

    /// <summary>
    ///     Completes a load with the number of items the file yielded.
    /// </summary>
    /// <param name="itemCount">The number of items the loaded file holds</param>
    /// <param name="hasUnsavedChanges">Whether the loaded items already carry unsaved changes</param>
    public void LoadSucceeded(int itemCount, bool hasUnsavedChanges)
    {
        Set(Current with
        {
            Phase = itemCount > 0 ? DocumentPhase.Loaded : DocumentPhase.Empty,
            ItemCount = itemCount,
            VisibleItemCount = itemCount,
            IsFiltering = false,
            HasUnsavedChanges = hasUnsavedChanges
        });
    }

    /// <summary>
    ///     Restores the state recorded before <see cref="BeginLoad" />.
    /// </summary>
    public void LoadFailed()
    {
        Set(stateBeforeLoad);
    }

    /// <summary>
    ///     Synchronizes the phase with the number of items in the open document.
    /// </summary>
    /// <param name="itemCount">The current number of items</param>
    public void UpdateItemCount(int itemCount)
    {
        if (Current.Phase is not (DocumentPhase.Loaded or DocumentPhase.Empty)) return;
        Set(Current with
        {
            Phase = itemCount > 0 ? DocumentPhase.Loaded : DocumentPhase.Empty,
            ItemCount = itemCount,
            VisibleItemCount = Math.Min(Current.VisibleItemCount, itemCount)
        });
    }

    /// <summary>
    ///     Records whether the open document holds unsaved changes.
    /// </summary>
    /// <param name="hasUnsavedChanges">True when at least one item was modified</param>
    public void SetUnsavedChanges(bool hasUnsavedChanges)
    {
        if (Current.Phase is not (DocumentPhase.Loaded or DocumentPhase.Empty)) return;
        Set(Current with { HasUnsavedChanges = hasUnsavedChanges });
    }

    /// <summary>
    ///     Records whether a search filter is active and how many rows it leaves visible.
    /// </summary>
    /// <param name="isFiltering">True when the search box holds a query</param>
    /// <param name="visibleItemCount">The number of rows the current filter leaves visible</param>
    public void SetFilter(bool isFiltering, int visibleItemCount)
    {
        if (Current.Phase is not (DocumentPhase.Loaded or DocumentPhase.Empty)) return;
        Set(Current with { IsFiltering = isFiltering, VisibleItemCount = visibleItemCount });
    }

    private void Set(DocumentState next)
    {
        if (next == Current) return;
        Current = next;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}