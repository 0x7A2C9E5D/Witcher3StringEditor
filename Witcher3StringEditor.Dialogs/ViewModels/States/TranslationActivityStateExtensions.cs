namespace Witcher3StringEditor.Dialogs.ViewModels.States;

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