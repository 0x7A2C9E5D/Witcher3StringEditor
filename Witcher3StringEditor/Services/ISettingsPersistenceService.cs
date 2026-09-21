namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for configuration file saving and loading operations
///     Provides methods to save and load settings to and from configuration files
/// </summary>
/// <remarks>
///     Implementations must be safe for concurrent use and persist atomically (write to a temporary file and
///     replace the target) so that a crash cannot leave a truncated configuration behind
/// </remarks>
internal interface ISettingsPersistenceService
{
    /// <summary>
    ///     Saves the specified settings to a configuration file
    /// </summary>
    /// <typeparam name="T">
    ///     The type of settings to save. Both methods use the same unconstrained type parameter so that any type
    ///     that can be saved can also be loaded
    /// </typeparam>
    /// <param name="settings">The settings to save</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings" /> is null</exception>
    /// <exception cref="System.IO.IOException">The configuration file could not be written</exception>
    /// <exception cref="UnauthorizedAccessException">The configuration file could not be written</exception>
    void Save<T>(T settings);

    /// <summary>
    ///     Loads settings from a configuration file
    /// </summary>
    /// <typeparam name="T">
    ///     The type of settings to load. It must expose a public parameterless constructor, because a default
    ///     instance is returned when the file is missing or invalid
    /// </typeparam>
    /// <returns>
    ///     The loaded settings, or a new default instance when the file does not exist, cannot be read or
    ///     contains invalid JSON
    /// </returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="T" /> cannot be constructed</exception>
    T Load<T>();
}