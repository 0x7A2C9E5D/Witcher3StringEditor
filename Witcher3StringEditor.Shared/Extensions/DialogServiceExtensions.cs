using System.ComponentModel;
using System.Windows;
using HanumanInstitute.MvvmDialogs;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Shared.Extensions;

/// <summary>
///     How important a notification is. View models use this instead of a platform icon enum;
///     the dialog layer maps it to the icon rendered by the current theme.
/// </summary>
public enum MessageBoxIcon
{
    /// <summary>Neutral information.</summary>
    Information,

    /// <summary>Something is off, but the flow can continue.</summary>
    Warning,

    /// <summary>An operation failed.</summary>
    Error
}

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
    /// <summary>
    ///     Shows a notification dialog that the user can choose to dismiss.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="owner"></param>
    /// <param name="content"></param>
    /// <param name="title"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    public static Task MessageBoxNotifyAsync(this IDialogService service,
        INotifyPropertyChanged owner,
        string content,
        string title,
        MessageBoxIcon severity = MessageBoxIcon.Information)
    {
        return Task.FromResult(Show(service, owner, content, title, MessageBoxButton.OK, severity));
    }

    /// <summary>
    ///     Shows a confirmation dialog that the user can choose to confirm or cancel.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="owner"></param>
    /// <param name="content"></param>
    /// <param name="title"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    public static Task<bool> MessageBoxConfirmAsync(this IDialogService service,
        INotifyPropertyChanged owner,
        string content,
        string title,
        MessageBoxIcon severity = MessageBoxIcon.Information)
    {
        return Task.FromResult(Show(service, owner, content, title, MessageBoxButton.YesNo, severity) ==
                               MessageBoxResult.Yes);
    }

    /// <summary>
    ///     Shows a message box with the specified content, caption, button, and icon.
    /// </summary>
    /// <param name="service"></param>
    /// <param name="owner"></param>
    /// <param name="content"></param>
    /// <param name="caption"></param>
    /// <param name="button"></param>
    /// <param name="severity"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    ///     Maps the <see cref="MessageBoxIcon" /> to the <see cref="MessageBoxImage" />
    /// </summary>
    /// <param name="icon"></param>
    /// <returns></returns>
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