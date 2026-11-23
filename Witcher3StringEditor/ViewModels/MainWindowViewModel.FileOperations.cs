using System.IO;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Shared.Extensions;
using Witcher3StringEditor.ViewModels.States;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Opening a file: asking for the file, loading its items and asking before another file is replaced.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Opens a file using a file dialog
    /// </summary>
    [RelayCommand]
    private async Task OpenFile()
    {
        // Show open file dialog
        using var storageFile = await dialogService.ShowOpenFileDialogAsync(this, CreateOpenFileDialogSettings());
        if (storageFile is not null &&
            IsSupportedFile(storageFile.LocalPath)) // Check if file has a supported extension
            await LoadFileAsync(storageFile.LocalPath); // Load the chosen file
    }

    /// <summary>
    ///     Loads the specified file and replaces the loaded items with its contents
    /// </summary>
    /// <param name="fileName">The path to the file to load</param>
    private async Task LoadFileAsync(string fileName)
    {
        try
        {
            if (!await HandleReOpenFile()) return; // Ask before the loaded items are replaced
            documentStateMachine.BeginLoad(); // Enter the loading state before the file is read
            W3StringItems = (await iw3StringSerializer.Deserialize(fileName)) // Deserialize file contents
                .Select(x => new W3StringItem(x)) // Wrap every entry in the view model item
                .OrderBy(x => x.StrId, StringComparer.InvariantCulture) // Keep the file order stable
                .ToObservableCollection(); // Materialize once for the data grid
            documentStateMachine.LoadSucceeded(W3StringItems.Count,
                modifiedItems.Count > 0); // Publish the loaded document and its change state
            OutputFolder = Path.GetDirectoryName(fileName)!; // Set output folder based on file location
            recentFilesService.AddOrUpdateRecentFile(fileName); // Update recent items list
            SearchText = string.Empty; // Clear search text
            await RequestDataGridPagedSource(); // Take over the paged source of the file that was opened
        }
        catch (Exception ex)
        {
            // Only a failure while reading rolls the state back: the later steps already run on the new document
            if (documentStateMachine.Current.Phase == DocumentPhase.Loading)
                documentStateMachine.LoadFailed();
            Log.Error(ex, "Failed to open file: {FileName}", fileName); // Log any errors during file opening
            await dialogService.MessageBoxNotifyAsync(this, Strings.OpenFileFailedMessage,
                Strings.OpenFileFailedCaption, MessageBoxIcon.Warning); // Tell the user the file could not be opened
        }
    }

    /// <summary>
    ///     Handles reopening a file when another file is already open
    /// </summary>
    /// <returns>True if the file can be reopened, false otherwise</returns>
    private async Task<bool> HandleReOpenFile()
    {
        if (W3StringItems?.Count is not > 0) return true; // Return true if no items currently loaded
        return await dialogService.MessageBoxConfirmAsync(this, Strings.ReOpenFileMessage,
            Strings.ReOpenFileCaption); // Ask whether the currently loaded file may be replaced
    }
}