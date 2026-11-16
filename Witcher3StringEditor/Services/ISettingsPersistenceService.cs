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
    /// <param name="includeSharedRecords">
    ///     When false, the replicated recent file and backup collections are taken from the file
    ///     on disk instead of the given instance, because only the Raft leader owns them
    /// </param>
    public void Save<T>(T settings, bool includeSharedRecords = true);

    /// <summary>
    ///     Loads settings from a configuration file
    /// </summary>
    /// <typeparam name="T">The type of settings to load</typeparam>
    /// <returns>The loaded settings</returns>
    public T Load<T>() where T : new();
}