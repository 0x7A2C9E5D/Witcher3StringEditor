namespace Witcher3StringEditor.State;

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