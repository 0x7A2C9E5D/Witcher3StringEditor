namespace Witcher3StringEditor.ViewModels.States;

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