using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for settings management operations
///     Provides a method to check and validate application settings
/// </summary>
public interface ISettingsManagerService
{
    /// <summary>
    ///     Checks and validates the current application settings
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task CheckSettings();
    
    /// <summary>
    ///     Gets the application settings instance
    /// </summary>
    IAppSettings AppSettings { get; }
}