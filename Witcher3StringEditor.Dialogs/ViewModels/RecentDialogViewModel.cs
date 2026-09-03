using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the recent files dialog window
///     Manages the display and interaction with recently opened files
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
public sealed partial class RecentDialogViewModel(IRecentFilesService recentFilesService, IDialogService dialogService)
    : DisposableViewModel, IModalDialogViewModel, ICloseable
{
    public ObservableCollection<IRecentFileEntry> RecentItems =>
        recentFilesService.RecentItems;

    /// <summary>
    ///     Event that is raised when the dialog requests to be closed
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;

    /// <summary>
    ///     Opens a recent file
    ///     Checks if the file exists and handles accordingly
    /// </summary>
    /// <param name="recentFileEntry">The recent item to open</param>
    [RelayCommand]
    private async Task Open(IRecentFileEntry recentFileEntry)
    {
        if (!File.Exists(recentFileEntry.FilePath)) // Check if file exists
            await HandleMissingFile(recentFileEntry); // Handle missing file
        else
            HandleExistingFile(recentFileEntry); // Handle existing file
    }

    /// <summary>
    ///     Handles opening an existing file
    ///     Closes the dialog and sends a message to open the file
    /// </summary>
    /// <param name="recentFileEntry">The recent item to open</param>
    private void HandleExistingFile(IRecentFileEntry recentFileEntry)
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
        _ = WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<string, bool>(recentFileEntry.FilePath),
            MessageTokens.RecentFileOpened);
    }

    /// <summary>
    ///     Handles the case when a recent file is missing
    ///     Notifies the user and removes the recent item if confirmed
    /// </summary>
    /// <param name="recentFileEntry">The recent item that is missing</param>
    private async Task HandleMissingFile(IRecentFileEntry recentFileEntry)
    {
        LogMissingFile(recentFileEntry.FilePath); // Log missing file
        if (await NotifyFileNotFound()) // If user confirms
            recentFilesService.RemoveRecentFile(recentFileEntry); // Remove the recent item (logged by the service)
    }

    /// <summary>
    ///     Notifies the user that a file was not found
    /// </summary>
    /// <returns>True if the user confirmed the notification, false otherwise</returns>
    private async Task<bool> NotifyFileNotFound()
    {
        return await dialogService.MessageBoxConfirmAsync(this, Strings.FileOpenedNoFoundMessage,
            Strings.FileOpenedNoFoundCaption, MessageBoxIcon.Warning);
    }

    /// <summary>
    ///     Logs an error when a recent file is missing
    /// </summary>
    /// <param name="filePath">The path of the missing file</param>
    private static void LogMissingFile(string filePath)
    {
        Log.Warning("The file {Path} for the recent item being opened does not exist.", filePath);
    }
}
