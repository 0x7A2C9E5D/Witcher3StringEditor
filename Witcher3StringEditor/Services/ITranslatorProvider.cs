using GTranslate.Translators;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides access to the registered translation services
///     Translators are registered as transient: each instance returned by <see cref="GetTranslator" />
///     is a fresh instance that the caller must dispose after use
/// </summary>
internal interface ITranslatorProvider
{
    /// <summary>
    ///     Gets the display names of all registered translators
    ///     Used by the settings dialog to present the available translator options
    /// </summary>
    /// <returns>The display names of the registered translators</returns>
    IReadOnlyList<string> GetTranslatorNames();

    /// <summary>
    ///     Resolves the translator instance matching the specified settings name
    /// </summary>
    /// <param name="name">The translator name stored in settings (e.g. "MicrosoftTranslator")</param>
    /// <returns>
    ///     A transient translator instance matching the name
    ///     The caller is responsible for disposing the returned instance
    /// </returns>
    ITranslator GetTranslator(string name);
}