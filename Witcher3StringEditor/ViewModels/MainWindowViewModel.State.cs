using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Document state: the session state machine, the item hooks and the single place that refreshes
///     command availability.
/// </summary>
internal partial class MainWindowViewModel
{
    private readonly DocumentStateMachine documentStateMachine = new(); // The document session state machine
    private readonly HashSet<W3StringItem> modifiedItems = []; // The items that currently hold unsaved text

    /// <summary>
    ///     Gets a value indicating whether a document with at least one item is loaded
    /// </summary>
    public bool HasW3StringItems => documentStateMachine.Current.CanModify;

    /// <summary>
    ///     Wires the state machine to the command refresh
    /// </summary>
    private void InitializeDocumentState()
    {
        documentStateMachine.StateChanged += OnDocumentStateChanged; // Refresh whenever the state changes
    }

    /// <summary>
    ///     Notifies the bound empty-state hint and refreshes every document command
    /// </summary>
    private void OnDocumentStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(HasW3StringItems)); // Update the empty-state hint
        RefreshCommandStates(); // Update every command that depends on the document
    }

    /// <summary>
    ///     Refreshes the commands whose availability is derived from the document state
    /// </summary>
    private void RefreshCommandStates()
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        MergeDataCommand.NotifyCanExecuteChanged();
        ShowSaveDialogCommand.NotifyCanExecuteChanged();
        ShowTranslateDialogCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    ///     Hooks the replaced collection and keeps the state in sync with the new one
    /// </summary>
    partial void OnW3StringItemsChanged(ObservableCollection<W3StringItem>? oldValue,
        ObservableCollection<W3StringItem>? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CollectionChanged -= OnW3StringItemsCollectionChanged;
            foreach (var item in oldValue) DetachItem(item); // Stop tracking the replaced document
        }

        if (newValue is not null)
        {
            newValue.CollectionChanged += OnW3StringItemsCollectionChanged;
            foreach (var item in newValue) AttachItem(item); // Track the newly opened document
        }

        SyncDocumentState(); // Publish the new item count and change flags
    }

    /// <summary>
    ///     Keeps the state in sync when items are added to or removed from the open document
    /// </summary>
    private void OnW3StringItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (W3StringItem item in e.OldItems)
                DetachItem(item); // Stop tracking the removed items
        if (e.NewItems is not null)
            foreach (W3StringItem item in e.NewItems)
                AttachItem(item); // Track the added items
        SyncDocumentState(); // Publish the new item count and change flags
    }

    /// <summary>
    ///     Starts tracking whether the given item holds unsaved text
    /// </summary>
    /// <param name="item">The item to track</param>
    private void AttachItem(W3StringItem item)
    {
        item.PropertyChanged += OnW3StringItemPropertyChanged;
        if (item.IsModified) modifiedItems.Add(item);
    }

    /// <summary>
    ///     Stops tracking whether the given item holds unsaved text
    /// </summary>
    /// <param name="item">The item to stop tracking</param>
    private void DetachItem(W3StringItem item)
    {
        item.PropertyChanged -= OnW3StringItemPropertyChanged;
        modifiedItems.Remove(item);
    }

    /// <summary>
    ///     Updates the unsaved-changes flag when an item starts or stops being modified
    /// </summary>
    private void OnW3StringItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(W3StringItem.IsModified) || sender is not W3StringItem item) return;
        if (item.IsModified) modifiedItems.Add(item);
        else modifiedItems.Remove(item);
        documentStateMachine.SetUnsavedChanges(modifiedItems.Count > 0);
    }

    /// <summary>
    ///     Publishes the current item count and unsaved-changes flag to the state machine
    /// </summary>
    private void SyncDocumentState()
    {
        documentStateMachine.UpdateItemCount(W3StringItems?.Count ?? 0);
        documentStateMachine.SetUnsavedChanges(modifiedItems.Count > 0);
        RefreshCommandStates();
    }

    /// <summary>
    ///     Publishes the visible row count whenever the data grid source changes
    /// </summary>
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPagedSourceChanged(IList<W3StringItem>? value)
    {
        documentStateMachine.SetFilter(!string.IsNullOrWhiteSpace(SearchText), PagedSource?.Count ?? 0);
        RefreshCommandStates();
    }

    /// <summary>
    ///     Publishes the filter state whenever the search text changes
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        documentStateMachine.SetFilter(!string.IsNullOrWhiteSpace(value), PagedSource?.Count ?? 0);
    }
}