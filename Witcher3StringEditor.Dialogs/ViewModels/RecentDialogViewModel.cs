using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the recent files dialog window
///     Manages the display and interaction with recently opened files
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
public sealed partial class RecentDialogViewModel : DisposableViewModel, IModalDialogViewModel, ICloseable
{
    private readonly IRecentFilesService recentFilesService;

    public RecentDialogViewModel(IRecentFilesService recentFilesService)
    {
        this.recentFilesService = recentFilesService;
        recentFilesService.RecentItems.CollectionChanged += OnRecentItemsOnCollectionChanged;
    }

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
    ///     Releases the resources used by the LogDialogViewModel
    ///     Unsubscribes from collection change events to prevent memory leaks
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) // Only unsubscribe from events when disposing managed resources
            recentFilesService.RecentItems.CollectionChanged -= OnRecentItemsOnCollectionChanged;
    }

    /// <summary>
    ///     Handles changes to the recent items collection
    ///     Logs information when items are removed from the collection
    /// </summary>
    /// <param name="sender">The recent items collection</param>
    /// <param name="e">The collection change event arguments</param>
    private static void OnRecentItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove)
            Log.Information("Recent items collection changed: {Count} items removed.",
                e.OldItems?.Count ?? 0);
    }

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
        if (await NotifyFileNotFound(recentFileEntry.FilePath)) // If user confirms
            TryRemoveRecentItem(recentFileEntry); // Try to remove the recent item
    }

    /// <summary>
    ///     Attempts to remove a recent item from the collection
    ///     Logs the result of the operation
    /// </summary>
    /// <param name="recentFileEntry">The recent item to remove</param>
    private void TryRemoveRecentItem(IRecentFileEntry recentFileEntry)
    {
        if (recentFilesService.RemoveRecentFile(recentFileEntry))
            Log.Information("The recent item for file {Path} has been removed.", recentFileEntry.FilePath);
        else
            Log.Error("The recent item for file {Path} could not be removed.", recentFileEntry.FilePath);
    }

    /// <summary>
    ///     Notifies the user that a file was not found
    /// </summary>
    /// <param name="filePath">The path of the file that was not found</param>
    /// <returns>True if the user confirmed the notification, false otherwise</returns>
    private static async Task<bool> NotifyFileNotFound(string filePath)
    {
        return await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<string, bool>(filePath),
            MessageTokens.OpenedFileNoFound);
    }

    /// <summary>
    ///     Logs an error when a recent file is missing
    /// </summary>
    /// <param name="filePath">The path of the missing file</param>
    private static void LogMissingFile(string filePath)
    {
        Log.Error("The file {Path} for the recent item being opened does not exist.", filePath);
    }
}