using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the delete data confirmation dialog
///     Handles user confirmation for deleting selected The Witcher 3 string items
///     Implements IModalDialogViewModel for dialog result handling and ICloseable for close notifications
/// </summary>
/// <param name="w3StringItems">The collection of The Witcher 3 string items to be deleted</param>
public partial class DeleteDataDialogViewModel(IEnumerable<IW3StringItem> w3StringItems)
    : ObservableObject, IModalDialogViewModel, ICloseable
{
    /// <summary>
    ///     Gets the collection of The Witcher 3 string items to be deleted
    /// </summary>
    public IEnumerable<IW3StringItem> W3StringItems { get; } = w3StringItems;

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
        DialogResult = true; // Set dialog result to true
        RequestClose?.Invoke(this, EventArgs.Empty); // Request close
        Log.Information("The selected W3Items have been deleted."); // Log deletion
    }

    /// <summary>
    ///     Handles the cancel action
    ///     Sets the dialog result to false and requests the dialog to close
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false; // Set dialog result to false
        RequestClose?.Invoke(this, EventArgs.Empty); // Request close
        Log.Information("The selected W3Items have not been deleted."); // Log cancellation
    }
}