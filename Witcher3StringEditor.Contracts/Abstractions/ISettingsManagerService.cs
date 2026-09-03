using System.ComponentModel;

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
    ///     Checks and validates the current application settings, informing the user about anything that is missing
    /// </summary>
    /// <param name="dialogOwner">
    ///     The view model owning the window the notifications are shown on top of
    /// </param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task CheckSettings(INotifyPropertyChanged dialogOwner);
}