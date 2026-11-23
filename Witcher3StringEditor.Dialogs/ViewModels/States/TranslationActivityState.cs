namespace Witcher3StringEditor.Dialogs.ViewModels.States;

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
    ///     The user canceled the translation, or the running call observed the cancellation.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     An unexpected error terminated the translation.
    /// </summary>
    Failed
}