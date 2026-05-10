namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for settings management operations
///     Provides a method to check and validate application settings
/// </summary>
public interface ISettingsManagerService
{
    /// <summary>
    ///     Gets the application settings instance
    /// </summary>
    IAppSettings AppSettings { get; }

    /// <summary>
    ///     Checks and validates the current application settings
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task CheckSettings();
}