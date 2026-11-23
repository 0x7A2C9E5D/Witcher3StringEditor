namespace Witcher3StringEditor.Dialogs.States;

/// <summary>
///     The execution state shared by single-item and batch translation.
/// </summary>
internal enum TranslationActivityState
{
    /// <summary>
    ///     No translation is running; the user can start one.
    /// </summary>
    Idle,

    /// <summary>
    ///     A translation call is running.
    /// </summary>
    Translating,

    /// <summary>
    ///     Cancellation was requested and the running call has not finished yet.
    /// </summary>
    Cancelling,

    /// <summary>
    ///     The translation finished and every item succeeded.
    /// </summary>
    Completed,

    /// <summary>
    ///     The batch translation finished but at least one item failed.
    /// </summary>
    CompletedWithFailures,

    /// <summary>
    ///     The user cancelled the translation, or the running call observed the cancellation.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     An unexpected error terminated the translation.
    /// </summary>
    Failed
}

/// <summary>
///     Helpers for <see cref="TranslationActivityState" />.
/// </summary>
internal static class TranslationActivityStateExtensions
{
    /// <summary>
    ///     Determines whether the state represents a running translation that still blocks editing.
    /// </summary>
    /// <param name="state">The state to inspect</param>
    /// <returns>
    ///     True for <see cref="TranslationActivityState.Translating" /> and
    ///     <see cref="TranslationActivityState.Cancelling" />
    /// </returns>
    public static bool IsBusy(this TranslationActivityState state)
    {
        return state is TranslationActivityState.Translating or TranslationActivityState.Cancelling;
    }
}

/// <summary>
///     Tracks the translation activity state and reports every actual transition.
/// </summary>
internal sealed class TranslationActivityStateMachine
{
    /// <summary>
    ///     Gets the current activity state.
    /// </summary>
    public TranslationActivityState Current { get; private set; } = TranslationActivityState.Idle;

    /// <summary>
    ///     Occurs after the state changed; the arguments are the previous and the current state.
    /// </summary>
    public event Action<TranslationActivityState, TranslationActivityState>? StateChanged;

    /// <summary>
    ///     Starts a translation unless one is already running.
    /// </summary>
    public void Begin()
    {
        if (Current.IsBusy()) return;
        Set(TranslationActivityState.Translating);
    }

    /// <summary>
    ///     Moves a running translation into the cancelling state.
    /// </summary>
    public void RequestCancel()
    {
        if (Current != TranslationActivityState.Translating) return;
        Set(TranslationActivityState.Cancelling);
    }

    /// <summary>
    ///     Completes a running translation.
    /// </summary>
    /// <param name="hasFailures">True when at least one item failed</param>
    public void Complete(bool hasFailures)
    {
        if (!Current.IsBusy()) return;
        Set(hasFailures
            ? TranslationActivityState.CompletedWithFailures
            : TranslationActivityState.Completed);
    }

    /// <summary>
    ///     Marks a running translation as cancelled.
    /// </summary>
    public void Cancel()
    {
        if (!Current.IsBusy()) return;
        Set(TranslationActivityState.Cancelled);
    }

    /// <summary>
    ///     Marks a running translation as failed.
    /// </summary>
    public void Fail()
    {
        if (!Current.IsBusy()) return;
        Set(TranslationActivityState.Failed);
    }

    /// <summary>
    ///     Returns to the idle state, for example after navigating to another item.
    /// </summary>
    public void Reset()
    {
        Set(TranslationActivityState.Idle);
    }

    private void Set(TranslationActivityState next)
    {
        if (next == Current) return;
        var previous = Current;
        Current = next;
        StateChanged?.Invoke(previous, next);
    }
}