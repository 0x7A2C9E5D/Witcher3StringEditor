using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Dialogs.ViewModels;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Dialog commands: each opens one dialog and hands it what it needs. The translation dialog also resolves its
///     own translator and dictionary service, and disposes the translator when the dialog closes.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Shows the backup dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowBackupDialog()
    {
        await dialogService.ShowDialogAsync(this,
            dialogViewModelFactory.CreateBackupDialog());
    }

    /// <summary>
    ///     Shows the save dialog
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task ShowSaveDialog()
    {
        await dialogService.ShowDialogAsync(this,
            dialogViewModelFactory.CreateSaveDialog(W3StringItems!, OutputFolder));
    }

    /// <summary>
    ///     Shows the log dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowLogDialog()
    {
        using var logDialogViewModel = dialogViewModelFactory.CreateLogDialog(); // Create log dialog view model
        await dialogService.ShowDialogAsync<LogDialogViewModel>(this, logDialogViewModel); // Show the log dialog
    }

    /// <summary>
    ///     Shows the settings dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowSettingsDialog()
    {
        var names = translatorProvider.GetTranslatorNames(); // Get translator names for the settings dialog
        await dialogService.ShowDialogAsync(this,
            dialogViewModelFactory.CreateSettingsDialog(names)); // Show the settings dialog
    }

    /// <summary>
    ///     Shows about dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowAbout()
    {
        await dialogService.ShowDialogAsync(this, // Show about dialog
            dialogViewModelFactory.CreateAboutDialog(
                new Dictionary<string, object?> // Create view model with application information
                {
                    { "Version", ThisAssembly.AssemblyInformationalVersion }, // Application version
                    { "BuildTime", BuildInformation.BuildAt.ToLocalTime() }, // Build time
                    {
                        "OS", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})"
                    }, // OS information
                    { "Runtime", RuntimeInformation.FrameworkDescription }, // Runtime information
                    {
                        "Package", DependencyContext.Default?
                            .RuntimeLibraries.Where(static x => x.Type == "package")
                            .OrderBy(x => x.Name, StringComparer.InvariantCulture)
                    }
                }));
    }

    /// <summary>
    ///     Shows the recent files dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowRecentDialog()
    {
        using var recentDialogViewModel =
            dialogViewModelFactory.CreateRecentDialog(); // Create recent dialog view model
        await dialogService.ShowDialogAsync(this, recentDialogViewModel); // Show the recent files dialog
    }

    /// <summary>
    ///     Determines whether translate dialog can be shown
    /// </summary>
    /// <returns>True if translate dialog can be shown, false otherwise</returns>
    private bool CanShowTranslateDialog()
    {
        return documentStateMachine.Current.CanTranslate; // Delegate to the document state
    }

    /// <summary>
    ///     Shows translation dialog
    /// </summary>
    /// <param name="selectedItem">The initially selected item in the dialog</param>
    [RelayCommand(CanExecute = nameof(CanShowTranslateDialog))]
    private async Task ShowTranslateDialog(IW3StringItem? selectedItem)
    {
        var itemsToUse = PagedSource ?? W3StringItems!; // Use filtered items if available
        var selectedIndex =
            selectedItem is not null ? itemsToUse.IndexOf(selectedItem) : 0; // Get the index of the selected item
        var translator = translatorProvider.GetTranslator(AppSettings.Translator); // Get the configured translator
        var isUseDictionary = AppSettings.Translator == "MicrosoftTranslator";
        var dictionaryService = isUseDictionary ? serviceProvider.GetRequiredService<IDictionaryService>() : null;
        var translationDialogViewModel = dialogViewModelFactory.CreateTranslationDialog(translator,
            [.. itemsToUse], selectedIndex, dictionaryService);
        await dialogService.ShowDialogAsync(this, translationDialogViewModel); // Show translation dialog
        if (translator is IDisposable disposable) disposable.Dispose(); // Dispose of the translator if it's disposable
    }

    /// <summary>
    ///     Shows the dictionary dialog
    /// </summary>
    [RelayCommand]
    private async Task ShowDictionaryDialog()
    {
        await dialogService.ShowDialogAsync(this,
            dialogViewModelFactory.CreateDictionaryManagerDialog()); // Show the dictionary dialog
    }
}