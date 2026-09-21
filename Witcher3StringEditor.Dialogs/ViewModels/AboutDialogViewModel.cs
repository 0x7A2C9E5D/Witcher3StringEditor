using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the About dialog window
///     Displays application information such as version, author, and other relevant details
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
/// <param name="aboutInfo">A dictionary containing information to display in about dialog</param>
public class AboutDialogViewModel(IReadOnlyDictionary<string, object?> aboutInfo)
    : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     Gets the dictionary containing information to display in about dialog
    /// </summary>
    public IReadOnlyDictionary<string, object?> AboutInfo => aboutInfo;

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;
}