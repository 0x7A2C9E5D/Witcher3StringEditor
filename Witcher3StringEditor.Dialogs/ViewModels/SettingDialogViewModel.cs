using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the settings dialog window
///     Manages application settings including paths, translators, and supported cultures
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
/// <param name="appSettings">Application settings service</param>
/// <param name="dialogService">Dialog service for showing file dialogs</param>
/// <param name="translators">Collection of available translators</param>
/// <param name="supportedCultures">Collection of supported cultures for localization</param>
public partial class SettingDialogViewModel(
    IAppSettings appSettings,
    IDialogService dialogService,
    IEnumerable<string> translators,
    IEnumerable<CultureInfo> supportedCultures)
    : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     Gets the application settings service
    /// </summary>
    public IAppSettings AppSettings { get; } = appSettings;

    /// <summary>
    ///     Gets the collection of available translators
    /// </summary>
    public IEnumerable<string> Translators { get; } = translators;

    /// <summary>
    ///     Gets the collection of supported cultures for localization
    /// </summary>
    public IEnumerable<CultureInfo> SupportedCultures { get; } = supportedCultures;

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;

    /// <summary>
    ///     Sets the path to the witcher3.exe file
    ///     Opens a file dialog to allow the user to select the witcher3.exe file
    /// </summary>
    [RelayCommand]
    private async Task SetGameExePath()
    {
        // Open a file dialog to allow the user to select the witcher3.exe file.
        var dialogSettings = new OpenFileDialogSettings
        {
            Filters = [new FileFilter("witcher3.exe", ".exe")], // Set the file filter.
            Title = Strings.SelectGameExe, // Set the dialog title.
            SuggestedFileName = "witcher3" // Set the suggested file name.
        };
        using var storageFile =
            await dialogService.ShowOpenFileDialogAsync(this, dialogSettings); // Open the file dialog.
        if (storageFile is { Name: "witcher3.exe" }) // If the selected file is witcher3.exe, set the path to the file.
        {
            AppSettings.GameExePath = storageFile.LocalPath; // Set the path to the file.
            Log.Information("Game path set to {Path}", storageFile.LocalPath); // Log the path.
        }
    }
}