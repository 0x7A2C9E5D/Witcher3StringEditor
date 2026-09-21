using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the delete data confirmation dialog
///     Handles user confirmation for deleting selected The Witcher 3 string items
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
public partial class DeleteDataDialogViewModel : ObservableObject, IModalDialogViewModel, ICloseable
{
    /// <summary>
    ///     Initializes a new instance of the DeleteDataDialogViewModel class
    /// </summary>
    /// <param name="w3StringItems">The collection of The Witcher 3 string items to be deleted</param>
    public DeleteDataDialogViewModel(IReadOnlyList<IW3StringItem> w3StringItems)
    {
        W3StringItems = w3StringItems;
    }

    /// <summary>
    ///     Gets the collection of The Witcher 3 string items to be deleted
    /// </summary>
    public IReadOnlyList<IW3StringItem> W3StringItems { get; }

    /// <summary>
    ///     Event that is raised when the dialog requests to be closed
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    ///     Gets the dialog result value
    ///     True if the user confirmed deletion, false if canceled
    /// </summary>
    public bool? DialogResult { get; private set; }

    /// <summary>
    ///     Handles the delete confirmation action
    ///     Sets the dialog result to true and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private void Delete()
    {
        Close(true); // Confirm the deletion
    }

    /// <summary>
    ///     Handles the cancel action
    ///     Sets the dialog result to false and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Close(false); // Cancel the deletion
    }

    /// <summary>
    ///     Sets the dialog result and requests the close
    /// </summary>
    /// <param name="dialogResult">True when the deletion was confirmed</param>
    private void Close(bool dialogResult)
    {
        DialogResult = dialogResult; // Set dialog result
        RequestClose?.Invoke(this, EventArgs.Empty); // Request close
    }
}