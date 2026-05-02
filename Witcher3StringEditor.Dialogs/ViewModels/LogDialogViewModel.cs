using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using HanumanInstitute.MvvmDialogs;
using Serilog.Events;
using Syncfusion.Data.Extensions;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Dialogs.Models;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the log dialog window
///     Manages the display of log events in the UI and synchronizes with the source log collection
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
public sealed class LogDialogViewModel
    : DisposableViewModel, IModalDialogViewModel
{
    /// <summary>
    ///     The source collection of log events to display
    /// </summary>
    private readonly ObservableCollection<LogEvent> sourceLogEvents;
    
    /// <summary>
    ///      The lock object used to synchronize access to the log events collection
    /// </summary>
    private readonly object logEventsLock = new();
    
    /// <summary>
    ///     Initializes a new instance of the LogDialogViewModel class
    /// </summary>
    /// <param name="logAccessService">The log access service</param>
    public LogDialogViewModel(ILogAccessService logAccessService)
    {
        sourceLogEvents = logAccessService.Logs; // Initialize the source collection
        BindingOperations.EnableCollectionSynchronization(LogEvents, logEventsLock); 
        // Subscribe to UI collection changes to sync deletions back to source collection
        LogEvents.CollectionChanged += OnLogEventsCollectionChanged;
        // Subscribe to source collection changes to sync new items to UI collection
        sourceLogEvents.CollectionChanged += OnSourceLogsCollectionChanged;
        // Add existing log events to the UI collection
        sourceLogEvents.ForEach(x => LogEvents.Add(new LogEventItemModel(x)));
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
    ///     Handles changes to the source log events collection
    ///     Adds new log events to the UI collection when items are added to the source collection
    /// </summary>
    /// <param name="sender">The source collection</param>
    /// <param name="e">The collection change event arguments</param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnSourceLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only handle Add actions with valid items
        if (e is not { Action: NotifyCollectionChangedAction.Add, NewItems: not null }) return;
        // Add each new item to the UI collection on the UI thread
        foreach (LogEvent item in e.NewItems)
            await Application.Current.Dispatcher.InvokeAsync(() => LogEvents.Add(new LogEventItemModel(item)));
    }

    /// <summary>
    ///     Handles changes to the UI log events collection
    ///     Removes log events from the source collection when items are removed from the UI collection
    /// </summary>
    /// <param name="sender">The UI collection</param>
    /// <param name="e">The collection change event arguments</param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnLogEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only handle Remove actions with valid items
        if (e is not { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }) return;
        // Remove each deleted item from the source collection on the UI thread
        foreach (LogEventItemModel item in e.OldItems)
            await Application.Current.Dispatcher.InvokeAsync(() => sourceLogEvents.Remove(item.EventEntry));
    }
    
    /// <summary>
    ///     Releases the resources used by the LogDialogViewModel
    ///     Unsubscribes from collection change events to prevent memory leaks
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return; // Only unsubscribe from events when disposing managed resources
        // Unsubscribe from UI collection changes to prevent memory leaks when dialog is closed
        LogEvents.CollectionChanged -= OnLogEventsCollectionChanged;
        // Unsubscribe from source collection changes to prevent memory leaks when dialog is closed
        sourceLogEvents.CollectionChanged -= OnSourceLogsCollectionChanged;
    }
}