using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Serilog.Events;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the log dialog window
///     Manages the display of log events in the UI and subscribes to the log access service for new events
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
public sealed partial class LogDialogViewModel
    : DisposableViewModel, IModalDialogViewModel
{
    /// <summary>
    ///     The application settings used to resolve external URLs
    /// </summary>
    private readonly IAppSettings appSettings;

    /// <summary>
    ///     The dialog service used to show notification messages
    /// </summary>
    private readonly IDialogService dialogService;

    /// <summary>
    ///     The log access service feeding new log events to this view model
    /// </summary>
    private readonly ILogAccessService logAccessService;

    /// <summary>
    ///     The shell open service used to open the log folder and external pages
    /// </summary>
    private readonly IShellOpenService shellOpenService;

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
        this.logAccessService = logAccessService;
        this.appSettings = appSettings;
        this.shellOpenService = shellOpenService;
        this.dialogService = dialogService;
        // Take the history and register for new events in one step: an event is either in the returned
        // history or delivered to OnLogAdded afterward, so the view never shows one twice nor misses one
        foreach (var logEvent in logAccessService.Subscribe(OnLogAdded))
            LogEvents.Add(new LogEventItemModel(logEvent));
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
    ///     Handles a log event delivered by the log access service
    ///     Appends it to the UI collection on the UI thread, because the service raises the event on the thread
    ///     that wrote the log and the collection is bound to the grid
    /// </summary>
    /// <param name="logEvent">The log event to display</param>
    private void OnLogAdded(LogEvent logEvent)
    {
        Application.Current.Dispatcher.BeginInvoke(() => LogEvents.Add(new LogEventItemModel(logEvent)));
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
    ///     Unsubscribes from the log access service to prevent memory leaks
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing); // Call the base class's Dispose method
        if (!disposing) return; // Only unsubscribe from events when disposing managed resources
        // Unsubscribe from new log events to prevent memory leaks when dialog is closed
        logAccessService.Unsubscribe(OnLogAdded);
    }
}