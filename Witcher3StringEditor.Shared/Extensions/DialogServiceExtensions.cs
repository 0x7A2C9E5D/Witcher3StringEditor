using System.ComponentModel;
using System.Windows;
using HanumanInstitute.MvvmDialogs;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Shared.Extensions;

/// <summary>
///     Adds themed message boxes to <see cref="IDialogService" />, so that view models can inform or
///     question the user themselves instead of asking the view to do it.
/// </summary>
/// <remarks>
///     The API deliberately avoids platform enumeration types such as
///     <c>System.Windows.MessageBoxImage</c>: those are mapped here, in the dialog layer, which is
///     the only place that is allowed to know about them.
/// </remarks>
public static class DialogServiceExtensions
{
    /// <summary>Shows a notification that the user acknowledges with a single OK button.</summary>
    public static Task MessageBoxNotifyAsync(this IDialogService service,
        INotifyPropertyChanged owner,
        string content,
        string title,
        MessageBoxIcon severity = MessageBoxIcon.Information)
    {
        return Task.FromResult(Show(service, owner, content, title, MessageBoxButton.OK, severity));
    }

    /// <summary>Asks the user a yes/no question.</summary>
    public static Task<bool> MessageBoxConfirmAsync(this IDialogService service,
        INotifyPropertyChanged owner,
        string content,
        string title,
        MessageBoxIcon severity = MessageBoxIcon.Information)
    {
        return Task.FromResult(Show(service, owner, content, title, MessageBoxButton.YesNo, severity) ==
                               MessageBoxResult.Yes);
    }

    private static MessageBoxResult Show(
        IDialogService service,
        INotifyPropertyChanged owner,
        string content,
        string caption,
        MessageBoxButton button,
        MessageBoxIcon severity)
    {
        ArgumentNullException.ThrowIfNull(owner);

        // The owner is resolved through the dialog manager to keep the box on top of the window
        // that owns the calling view model, matching how the other framework dialogs behave.
        return service.DialogManager.FindViewByViewModel(owner)?.RefObj is not Window ownerWindow
            ? MessageBox.Show(content, caption, button, MapIcon(severity))
            : MessageBox.Show(ownerWindow, content, caption, button, MapIcon(severity));
    }

    private static MessageBoxImage MapIcon(MessageBoxIcon icon)
    {
        return icon switch
        {
            MessageBoxIcon.Error => MessageBoxImage.Error,
            MessageBoxIcon.Warning => MessageBoxImage.Warning,
            _ => MessageBoxImage.Information
        };
    }
}