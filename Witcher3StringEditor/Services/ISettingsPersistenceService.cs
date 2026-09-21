namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for configuration file saving and loading operations
///     Provides methods to save and load settings to and from configuration files
/// </summary>
internal interface ISettingsPersistenceService
{
    /// <summary>
    ///     Saves the specified settings to a configuration file
    /// </summary>
    /// <typeparam name="T">The type of settings to save</typeparam>
    /// <param name="settings">The settings to save</param>
    public void Save<T>(T settings);

    /// <summary>
    ///     Loads settings from a configuration file
    /// </summary>
    /// <typeparam name="T">The type of settings to load</typeparam>
    /// <returns>The loaded settings</returns>
    public T Load<T>() where T : new();
}