using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;

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
    ///     Gets the title for the dialog window
    ///     Displays "Add" if the item is new (no StrId), or "Edit" if editing an existing item
    /// </summary>
    public string Title { get; } = string.IsNullOrWhiteSpace(w3StringItem.StrId)
        ? Strings.AddDialogTitle
        : Strings.EditDialogTitle;

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
    private void Submit()
    {
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