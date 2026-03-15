// ... existing code ...

using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using iNKORE.UI.WPF.Modern.Controls;
using Serilog;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Messaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for BackupDialog.xaml
///     This dialog displays backup files and allows users to restore or delete them
/// </summary>
public partial class BackupDialog
{
    /// <summary>
    ///     Initializes a new instance of the BackupDialog class
    ///     Sets up the UI components, search helper, and message handlers
    /// </summary>
    public BackupDialog()
    {
        InitializeComponent(); // InitializeComponent
        SetupSearchHelper(); // Setup search helper
        RegisterMessageHandlers(); // Register message handlers
    }

    /// <summary>
    ///     Sets up the search helper for the data grid
    ///     Enables filtering and disables case-sensitive search
    /// </summary>
    private void SetupSearchHelper()
    {
        SfDataGrid.SearchHelper.AllowFiltering = true;
        SfDataGrid.SearchHelper.AllowCaseSensitiveSearch = false;
        SfDataGrid.SearchHelper.CanHighlightSearchText = false;
    }

    /// <summary>
    ///     Registers message handlers for various operations in the backup dialog
    ///     These handlers respond to messages sent from view models to show confirmation dialogs
    /// </summary>
    private void RegisterMessageHandlers()
    {
        var messageHandlers = CreateMessageHandlers();
        foreach (var (token, message, caption, button, icon, expected) in messageHandlers)
            RegisterMessageHandler(token, message, caption, button, icon, expected);
    }

    /// <summary>
    ///     Registers a single message handler for a specific token
    ///     Shows a message box with the specified parameters and replies with the user's choice
    /// </summary>
    /// <param name="token">The message token to listen for</param>
    /// <param name="message">Function that returns the message text</param>
    /// <param name="caption">Function that returns the caption text</param>
    /// <param name="button">The buttons to display in the message box</param>
    /// <param name="icon">The icon to display in the message box</param>
    /// <param name="expected">The expected result for a positive response</param>
    private void RegisterMessageHandler(string token, Func<string> message, Func<string> caption,
        MessageBoxButton button, MessageBoxImage icon,
        MessageBoxResult expected)
    {
        WeakReferenceMessenger.Default.Register<BackupDialog, AsyncRequestMessage<bool>, string>(
            this,
            token,
            (_, m) => { m.Reply(MessageBox.Show(message(), caption(), button, icon) == expected); });
    }

    /// <summary>
    ///     Creates an array of message handler configurations
    ///     Each configuration defines a message box that can be shown for a specific operation
    /// </summary>
    /// <returns>An array of message handler configurations</returns>
    private static (string, Func<string>, Func<string>, MessageBoxButton, MessageBoxImage, MessageBoxResult)[]
        CreateMessageHandlers()
    {
        return
        [
            (MessageTokens.BackupRestore, () => Strings.BackupRestoreMessage, () => Strings.BackupRestoreCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.Yes),
            (MessageTokens.BackupDelete, () => Strings.BackupDeleteMessage, () => Strings.BackupDeleteCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.Yes),
            (MessageTokens.BackupFileNoFound, () => Strings.BackupFileNoFoundMessage,
                () => Strings.BackupFileNoFoundCaption,
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes),
            (MessageTokens.OperationFailed, () => Strings.OperationFailureMessage, () => Strings.OperationResultCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Warning, MessageBoxResult.OK)
        ];
    }

    /// <summary>
    ///     Handles the query submitted event of the search box
    ///     Performs a search in the data grid based on the entered query text
    /// </summary>
    /// <param name="sender">The auto suggest box that triggered the event</param>
    /// <param name="args">The event arguments containing the query text</param>
    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.QueryText)) return;
        SfDataGrid.SearchHelper.Search(args.QueryText);
        Log.Information("Search query submitted: {QueryText}", args.QueryText);
    }

    /// <summary>
    ///     Handles the text changed event of the search box
    ///     Clears the search when the text is empty or null
    /// </summary>
    /// <param name="sender">The auto suggest box that triggered the event</param>
    /// <param name="args">The event arguments containing information about the text change</param>
    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(sender.Text))
            SfDataGrid.SearchHelper.ClearSearch();
    }

    /// <summary>
    ///     Handles the closed event of the backup dialog
    ///     Unregisters message handlers and disposes of resources to prevent memory leaks
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void BackupDialog_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        SfDataGrid.SearchHelper.Dispose();
        SfDataGrid.Dispose();
        SfDataPager.Dispose();
    }
}