using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HanumanInstitute.MvvmDialogs;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the edit data dialog window
///     Handles adding or editing a single The Witcher 3 string item
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
/// <param name="w3StringItem">The Witcher 3 string item to edit or use as a template for adding a new item</param>
public partial class EditDataDialogViewModel(ITrackableW3StringItem w3StringItem)
    : ObservableObject, IModalDialogViewModel, ICloseable
{
    /// <summary>
    ///     Gets whether the item is new (no StrId)
    /// </summary>
    private bool IsNewItem => string.IsNullOrWhiteSpace(w3StringItem.StrId);

    /// <summary>
    ///     Gets the dialog title
    /// </summary>
    public string Title => IsNewItem ? Strings.AddDialogTitle : Strings.EditDialogTitle;

    /// <summary>
    ///     Gets a clone of The Witcher 3 string item being edited
    ///     This allows editing without affecting the original item until changes are confirmed
    /// </summary>
    public ITrackableW3StringItem? Item { get; } = w3StringItem.Clone() as ITrackableW3StringItem;

    /// <summary>
    ///     Event that is raised when the dialog requests to be closed
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///     Gets the dialog result value
    ///     True if the user submitted changes, false if canceled
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    ///     Handles the submit action
    ///     Sets the dialog result to true and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(w3StringItem.StrId)) return;
        var duplicateCount = await WeakReferenceMessenger.Default.Send(
            new AsyncRequestMessage<ITrackableW3StringItem, int>(w3StringItem));
        var hasDuplicate = IsNewItem ? duplicateCount > 0 : duplicateCount > 1;
        if (hasDuplicate) return;
        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Handles the cancel action
    ///     Sets the dialog result to false and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}