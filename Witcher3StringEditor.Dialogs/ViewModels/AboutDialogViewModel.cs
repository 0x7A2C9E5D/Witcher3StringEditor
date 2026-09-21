using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the About dialog window
///     Displays application information such as version, author, and other relevant details
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
public class AboutDialogViewModel : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     Initializes a new instance of the AboutDialogViewModel class
    /// </summary>
    /// <param name="aboutInfo">The information to display in the about dialog; the caller may keep mutating it</param>
    /// <exception cref="ArgumentNullException"><paramref name="aboutInfo" /> is null</exception>
    public AboutDialogViewModel(IReadOnlyDictionary<string, object?> aboutInfo)
    {
        ArgumentNullException.ThrowIfNull(aboutInfo);
        // The entries are copied so a caller that keeps mutating the source cannot change what the dialog renders
        AboutInfo = aboutInfo;
    }

    /// <summary>
    ///     Gets the immutable snapshot of the information displayed in the about dialog
    /// </summary>
    public IReadOnlyDictionary<string, object?> AboutInfo { get; }

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;
}