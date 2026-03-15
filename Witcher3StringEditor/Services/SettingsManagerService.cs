using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides settings management functionality
///     Implements the ISettingsManagerService interface to handle checking and applying application settings
/// </summary>
internal class SettingsManagerService : ISettingsManagerService
{
    /// <summary>
    ///     The application settings instance
    /// </summary>
    private readonly IAppSettings appSettings;

    /// <summary>
    ///     Initializes a new instance of the SettingsManagerService class
    /// </summary>
    /// <param name="appSettings">The application settings instance</param>
    public SettingsManagerService(IAppSettings appSettings)
    {
        this.appSettings = appSettings; // Store the app settings instance
        if (this.appSettings is INotifyPropertyChanged
            notifyPropertyChanged) // Check if app settings supports property change notifications
            notifyPropertyChanged.PropertyChanged += OnAppSettingsPropertyChanged; // Register property change handler
    }

    /// <summary>
    ///     Checks the current application settings and logs information about them
    ///     If required settings are missing, sends a message to trigger the first run setup
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task CheckSettings()
    {
        Log.Information("Checking whether the settings are correct."); //Log checking message
        if (await CheckRequiredSettings()) return; // Check required settings
        var hasErrors = false; // Create a flag to indicate whether there are errors
        hasErrors |= !ValidateW3StringsPath(appSettings); // Validate W3Strings path
        hasErrors |= !ValidateGameExePath(appSettings); // Validate game executable path
        LogAdditionalSettings(appSettings); // Log additional settings
        await HandleValidationResult(hasErrors); // Handle validation result
    }

    /// <summary>
    ///     Checks if required settings are present and triggers first run if not
    /// </summary>
    /// <returns>True if first run setup was triggered, otherwise false</returns>
    private async Task<bool> CheckRequiredSettings()
    {
        if (!string.IsNullOrWhiteSpace(appSettings.W3StringsPath)) return false; // Check if W3Strings path is set
        Log.Error(
            "Settings are incorrect or initial setup is incomplete."); // Log settings incorrect or incomplete message
        _ = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
            MessageTokens.FirstRun); // Trigger first run setup
        return true; // Return true if first run was triggered
    }

    /// <summary>
    ///     Validates the W3Strings path setting
    /// </summary>
    /// <param name="appSettings">The application settings instance</param>
    /// ///
    /// <returns></returns>
    private static bool ValidateW3StringsPath(IAppSettings appSettings)
    {
        if (!File.Exists(appSettings.W3StringsPath)) // Check if file exists
        {
            Log.Error("The W3Strings path is invalid: {Path}",
                appSettings.W3StringsPath); // Log w3strings path invalid message
            return false; // Return false if file does not exist
        }

        Log.Information("The W3Strings path has been set to {Path}.",
            appSettings.W3StringsPath); // Log valid w3strings path message
        return true; // Return true if file exists
    }

    /// <summary>
    ///     Validates the game executable path setting
    /// </summary>
    /// <param name="appSettings">The application settings instance</param>
    /// ///
    /// <returns></returns>
    private static bool ValidateGameExePath(IAppSettings appSettings)
    {
        if (!string.IsNullOrWhiteSpace(appSettings.GameExePath)) // Check if game executable path is set
        {
            if (!File.Exists(appSettings.GameExePath)) // Check if game executable exists
            {
                Log.Error("The game executable path is invalid: {Path}",
                    appSettings.GameExePath); // Log game executable path invalid message
                return false; // Return false if game executable does not exist
            }

            Log.Information("The game executable path has been set to {Path}.",
                appSettings.GameExePath); // Log valid game executable path message
            return true; // Return true if game executable exists
        }

        Log.Warning("The game executable path is unset."); // Log game executable path unset message
        return true; // Return true if game executable path is unset
    }

    /// <summary>
    ///     Logs additional settings
    /// </summary>
    /// <param name="appSettings">The application settings instance</param>
    private static void LogAdditionalSettings(IAppSettings appSettings)
    {
        Log.Information("The preferred filetype is {Filetype}",
            appSettings.PreferredW3FileType); // Log preferred filetype
        Log.Information("The preferred language is {Language}",
            appSettings.PreferredLanguage); // Log preferred language
        Log.Information("Current translator is {Translator}.", appSettings.Translator); // Log current translator
    }

    /// <summary>
    ///     Handles the result of settings validation
    /// </summary>
    /// <param name="hasErrors">Whether validation found errors</param>
    private static async Task HandleValidationResult(bool hasErrors)
    {
        if (hasErrors) // If there are errors
            _ = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.PathInvalid); // Trigger path invalid message
        else
            Log.Information("Settings are correct."); // Log settings correct message
    }

    /// <summary>
    ///     Handles the PropertyChanged event of the AppSettings object
    ///     Sends messages based on which property changed
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">A PropertyChangedEventArgs that contains the event data</param>
    private void OnAppSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Handle different property changes with appropriate actions
        switch (e.PropertyName) // Switch on property name
        {
            case nameof(IAppSettings.W3StringsPath): // If W3StringsPath changed
                _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), // Send message
                    MessageTokens.W3StringsPathChanged);
                break;
            case nameof(IAppSettings.GameExePath): // If GameExePath changed
                _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), // Send message
                    MessageTokens.GameExePathChanged);
                break;
            case nameof(IAppSettings.Translator): // If Translator changed
                ApplyTranslatorChange(appSettings); // Apply translator change
                break;
            case nameof(IAppSettings.Language): // If Language changed
                ApplyLanguageChange(appSettings.Language); // Apply language change
                break;
            case nameof(IAppSettings.PageSize): // If PageSize changed
                _ = WeakReferenceMessenger.Default.Send(
                    new ValueChangedMessage<int>(((IAppSettings)sender!).PageSize), // Send message
                    MessageTokens.PageSizeChanged);
                break;
        }
    }

    /// <summary>
    ///     Applies the translator change and logs the new translator
    /// </summary>
    /// <param name="appSettings">The application settings instance</param>
    private static void ApplyTranslatorChange(IAppSettings appSettings)
    {
        Log.Information("Translator changed to {Translator}", appSettings.Translator);
        _ = WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>(appSettings.Translator),
            MessageTokens.TranslatorChanged); // Send message
    }

    /// <summary>
    ///     Applies the language change and updates the culture
    /// </summary>
    /// <param name="language">The new language code</param>
    private static void ApplyLanguageChange(string language)
    {
        try
        {
            I18NExtension.Culture = new CultureInfo(language); // Set the new culture
            Log.Information("Language changed to {Language}.", language); // Log successful language change
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change language."); // Log any errors during language change
        }
    }
}