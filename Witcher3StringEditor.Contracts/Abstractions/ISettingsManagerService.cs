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
    /// <param name="cancellationToken">A token used to abort the validation and the user notifications</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled</exception>
    Task CheckSettings(INotifyPropertyChanged dialogOwner, CancellationToken cancellationToken = default);
}