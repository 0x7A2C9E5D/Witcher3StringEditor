using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Services;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Window lifecycle: what happens when the main window is loaded and when it is asked to close.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Handles the window loaded event
    ///     Logs application startup information and checks for updates
    /// </summary>
    [RelayCommand]
    private async Task WindowLoaded()
    {
        await settingsManagerService.CheckSettings(this); // Check application settings
        await using var scope = serviceProvider.CreateAsyncScope(); // Create a new scope
        IsUpdateAvailable = await scope.ServiceProvider
            .GetRequiredService<ICheckUpdateService>().CheckUpdate(); // Check for updates
    }

    /// <summary>
    ///     Handles the window closing event
    ///     Cancels the close if there are unsaved changes
    /// </summary>
    /// <param name="e">The event arguments for the cancel event</param>
    [RelayCommand]
    private async Task WindowClosing(CancelEventArgs e)
    {
        if (W3StringItems?.Any(x => x.IsModified) == true && // Check if there are any W3String items
            !await dialogService.MessageBoxConfirmAsync(this, Strings.AppExitMessage,
                Strings.AppExitCaption)) // Ask the user before discarding the changes
            e.Cancel = true; // Cancel window closing if requested
    }
}