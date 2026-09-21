using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for RecentDialog.xaml
///     This dialog displays recently opened files and allows users to open or manage them
/// </summary>
public partial class RecentDialog
{
    /// <summary>
    ///     Initializes a new instance of the RecentDialog class
    ///     Sets up the UI components and search helper
    /// </summary>
    public RecentDialog()
    {
        InitializeComponent(); // Initialize the UI components
        RegisterMessageHandler(); // Register message handler
    }

    /// <summary>
    ///     Registers the message handler for the recent dialog
    /// </summary>
    private void RegisterMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<RecentDialog, AsyncRequestMessage<bool>, string>(
            this, MessageTokens.RecentItem, (_, m) =>
            {
                m.Reply(MessageBox.Show(Strings.RecordDeletingMessgae,
                    Strings.RecordDeletingCaption,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes);
            });
    }
}