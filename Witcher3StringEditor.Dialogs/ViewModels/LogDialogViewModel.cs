using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Serilog.Events;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the log dialog window
///     Manages the display of log events in the UI and synchronizes with the source log collection
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
public sealed partial class LogDialogViewModel
    : DisposableViewModel, IModalDialogViewModel
{
    /// <summary>
    ///     The lock object used to synchronize access to the log events collection
    /// </summary>
    private readonly object logEventsLock = new();

    /// <summary>
    ///     The source collection of log events to display
    /// </summary>
    private readonly ObservableCollection<LogEvent> sourceLogEvents;

    /// <summary>
    ///     The shell open service used to open the log folder and external pages
    /// </summary>
    private readonly IShellOpenService shellOpenService;

    /// <summary>
    ///     The application settings used to resolve external URLs
    /// </summary>
    private readonly IAppSettings appSettings;

    /// <summary>
    ///     The dialog service used to show notification messages
    /// </summary>
    private readonly IDialogService dialogService;

    /// <summary>
    ///     Initializes a new instance of the LogDialogViewModel class
    /// </summary>
    /// <param name="logAccessService">The log access service</param>
    /// <param name="appSettings">The application settings</param>
    /// <param name="shellOpenService">The shell open service</param>
    /// <param name="dialogService">The dialog service</param>
    public LogDialogViewModel(ILogAccessService logAccessService, IAppSettings appSettings,
        IShellOpenService shellOpenService, IDialogService dialogService)
    {
        this.appSettings = appSettings;
        this.shellOpenService = shellOpenService;
        this.dialogService = dialogService;
        sourceLogEvents = logAccessService.Logs; // Initialize the source collection
        BindingOperations.EnableCollectionSynchronization(LogEvents, logEventsLock);
        // Subscribe to UI collection changes to sync deletions back to source collection
        LogEvents.CollectionChanged += OnLogEventsCollectionChanged;
        // Subscribe to source collection changes to sync new items to UI collection
        sourceLogEvents.CollectionChanged += OnSourceLogsCollectionChanged;
        InitializeExistingLogs(); // Initialize existing logs
    }

    /// <summary>
    ///     Gets the collection of log events for display in the UI
    /// </summary>
    public ObservableCollection<LogEventItemModel> LogEvents { get; } = [];

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;

    /// <summary>
    ///     Initializes the log events collection with existing log events
    /// </summary>
    private void InitializeExistingLogs()
    {
        lock (logEventsLock)
        {
            var models = sourceLogEvents
                .Select(x => new LogEventItemModel(x)).ToList();
            foreach (var model in models)
                LogEvents.Add(model);
        }
    }

    /// <summary>
    ///     Handles changes to the source log events collection
    ///     Adds new log events to the UI collection when items are added to the source collection
    /// </summary>
    /// <param name="sender">The source collection</param>
    /// <param name="e">The collection change event arguments</param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private void OnSourceLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only handle Add actions with valid items
        if (e is not { Action: NotifyCollectionChangedAction.Add, NewItems: not null }) return;
        // Add each new item to the UI collection on the UI thread
        foreach (LogEvent item in e.NewItems)
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (logEventsLock)
                {
                    LogEvents.Add(new LogEventItemModel(item));
                }
            });
    }

    /// <summary>
    ///     Handles changes to the UI log events collection
    ///     Removes log events from the source collection when items are removed from the UI collection
    /// </summary>
    /// <param name="sender">The UI collection</param>
    /// <param name="e">The collection change event arguments</param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private void OnLogEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only handle Remove actions with valid items
        if (e is not { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }) return;
        // Remove each deleted item from the source collection on the UI thread
        foreach (LogEventItemModel item in e.OldItems)
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (logEventsLock)
                {
                    sourceLogEvents.Remove(item.EventEntry);
                }
            });
    }

    /// <summary>
    ///     Opens the log folder
    /// </summary>
    [RelayCommand]
    private void OpenLogFolder()
    {
        shellOpenService.Open(AppPaths.LogDirectory); // Open the log folder.
        Log.Information("Opened log folder"); // Log that the log folder has been opened.
    }

    /// <summary>
    ///     Deletes old log files
    /// </summary>
    [RelayCommand]
    private async Task DeleteOldLogs()
    {
        var files = Directory.GetFiles(AppPaths.LogDirectory); // Get all log files in the log folder.
        if (files.Length == 1) // If there is only one log file, do nothing.
        {
            Log.Information("No log cleanup needed: only one log file exists"); // Log that only one log file exists
            await dialogService.MessageBoxNotifyAsync(this, Strings.LogsNoNeedToCleanMessage,
                Strings.LogCleanupCaption); // Tell the user that there is nothing to clean up.
            return;
        }

        var filesToDelete =
            files.OrderByDescending(File.GetLastWriteTime)
                .Skip(1); // Get all log files in the log folder, ordered by last write time, and skip the first one.

        var deletedFilesCount = 0; // Initialize the deleted files count.
        foreach (var file in filesToDelete) // Loop through all log files in the log folder.
            try
            {
                File.Delete(file); // Delete the log file.
                deletedFilesCount++; // Increment the deleted files count.
                Log.Information("Deleted log file: {Path}", file); // Log the deletion.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete log file: {Path}", file); // Log the error.
            }

        Log.Information("Deleted {Count} log files", deletedFilesCount); // Log the number of deleted log files.
        await dialogService.MessageBoxNotifyAsync(this, Strings.LogsCleanedMessage,
            Strings.LogCleanupCaption); // Tell the user that the log files have been cleaned.
    }

    /// <summary>
    ///     Opens the NexusMods bugs page where issues can be reported
    /// </summary>
    [RelayCommand]
    private void ReportBug()
    {
        shellOpenService.Open($"{appSettings.NexusModUrl}?tab=bugs"); // Open the bugs page.
        Log.Information("Opened NexusMods bugs page"); // Log that the bugs page has been opened.
    }

    /// <summary>
    ///     Releases the resources used by the LogDialogViewModel
    ///     Unsubscribes from collection change events to prevent memory leaks
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing); // Call the base class's Dispose method
        if (!disposing) return; // Only unsubscribe from events when disposing managed resources
        // Unsubscribe from UI collection changes to prevent memory leaks when dialog is closed
        LogEvents.CollectionChanged -= OnLogEventsCollectionChanged;
        // Unsubscribe from source collection changes to prevent memory leaks when dialog is closed
        sourceLogEvents.CollectionChanged -= OnSourceLogsCollectionChanged;
    }
}