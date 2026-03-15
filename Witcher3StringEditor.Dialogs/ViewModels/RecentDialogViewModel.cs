using System.Collections.Specialized;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
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
public sealed partial class RecentDialogViewModel : ObservableObject, IModalDialogViewModel, ICloseable, IDisposable
{
    /// <summary>
    ///     Tracks whether the object has been disposed to prevent multiple disposals
    ///     Set to true when Dispose method is called
    /// </summary>
    private bool disposedValue;

    /// <summary>
    ///     Initializes a new instance of the RecentDialogViewModel class
    /// </summary>
    /// <param name="appSettings">Application settings containing the recent items collection</param>
    public RecentDialogViewModel(IAppSettings appSettings)
    {
        AppSettings = appSettings;
        AppSettings.RecentItems.CollectionChanged +=
            OnRecentItemsOnCollectionChanged; // Subscribe to collection change events
    }

    /// <summary>
    ///     Gets the application settings service
    /// </summary>
    public IAppSettings AppSettings { get; }

    /// <summary>
    ///     Event that is raised when the dialog requests to be closed
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///     Releases all resources used by the LogDialogViewModel
    ///     Calls the protected Dispose method with disposing parameter set to true
    /// </summary>
    public void Dispose()
    {
        Dispose(true); // Dispose of managed resources
    }

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
    private void Dispose(bool disposing)
    {
        if (disposedValue) return; // Return if resources have already been disposed
        if (disposing) // Only unsubscribe from events when disposing managed resources
            AppSettings.RecentItems.CollectionChanged -= OnRecentItemsOnCollectionChanged;
        disposedValue = true; // Mark resources as disposed
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
    /// <param name="recentItem">The recent item to open</param>
    [RelayCommand]
    private async Task Open(IRecentItem recentItem)
    {
        if (!File.Exists(recentItem.FilePath)) // Check if file exists
            await HandleMissingFile(recentItem); // Handle missing file
        else
            HandleExistingFile(recentItem); // Handle existing file
    }

    /// <summary>
    ///     Handles opening an existing file
    ///     Closes the dialog and sends a message to open the file
    /// </summary>
    /// <param name="recentItem">The recent item to open</param>
    private void HandleExistingFile(IRecentItem recentItem)
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
        _ = WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<string, bool>(recentItem.FilePath),
            MessageTokens.RecentFileOpened);
    }

    /// <summary>
    ///     Handles the case when a recent file is missing
    ///     Notifies the user and removes the recent item if confirmed
    /// </summary>
    /// <param name="recentItem">The recent item that is missing</param>
    private async Task HandleMissingFile(IRecentItem recentItem)
    {
        LogMissingFile(recentItem.FilePath); // Log missing file
        if (await NotifyFileNotFound(recentItem.FilePath)) // If user confirms
            TryRemoveRecentItem(recentItem); // Try to remove the recent item
    }

    /// <summary>
    ///     Attempts to remove a recent item from the collection
    ///     Logs the result of the operation
    /// </summary>
    /// <param name="recentItem">The recent item to remove</param>
    private void TryRemoveRecentItem(IRecentItem recentItem)
    {
        if (AppSettings.RecentItems.Remove(recentItem))
            Log.Information("The recent item for file {Path} has been removed.", recentItem.FilePath);
        else
            Log.Error("The recent item for file {Path} could not be removed.", recentItem.FilePath);
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