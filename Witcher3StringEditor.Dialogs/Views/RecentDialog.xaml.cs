using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using iNKORE.UI.WPF.Modern.Controls;
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
    ///     Sets up the UI components, search helper, and message handlers
    /// </summary>
    public RecentDialog()
    {
        InitializeComponent(); // Initialize the UI components
        SetupSearchHelper(); // Setup search helper
        RegisterMessageHandler(); // Register message handler
    }

    /// <summary>
    ///     Registers a message handler for recent item operations
    ///     Shows a confirmation dialog when a recent item is about to be deleted
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

    /// <summary>
    ///     Sets up the search helper for the data grid
    ///     Enables filtering and disables case-sensitive search
    /// </summary>
    private void SetupSearchHelper()
    {
        SfDataGrid.SearchHelper.AllowFiltering = true; // Enable filtering
        SfDataGrid.SearchHelper.AllowCaseSensitiveSearch = false; // Disable case-sensitive search
        SfDataGrid.SearchHelper.CanHighlightSearchText = false; // Disable text highlighting
    }

    /// <summary>
    ///     Handles the query submitted event of the search box
    ///     Performs a search in the data grid based on the entered query text
    /// </summary>
    /// <param name="sender">The auto suggest box that triggered the event</param>
    /// <param name="args">The event arguments containing the query text</param>
    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Check if the query text is empty or null
        if (string.IsNullOrWhiteSpace(args.QueryText)) return;
        SfDataGrid.SearchHelper.Search(args.QueryText); // Perform a search in the data grid
    }

    /// <summary>
    ///     Handles the text changed event of the search box
    ///     Clears the search when the text is empty or null
    /// </summary>
    /// <param name="sender">The auto suggest box that triggered the event</param>
    /// <param name="args">The event arguments containing information about the text change</param>
    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Check if the text is empty or null
        if (string.IsNullOrEmpty(sender.Text))
            SfDataGrid.SearchHelper.ClearSearch(); // Clear the search
    }

    /// <summary>
    ///     Handles the closed event of the recent dialog
    ///     Unregisters message handlers and disposes of resources to prevent memory leaks
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void RecentDialog_OnClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this); // Unregister message handlers
        SfDataGrid.SearchHelper.Dispose(); // Dispose the search helper
        SfDataGrid.Dispose(); // Dispose the data grid
        SfDataPager.Dispose(); // Dispose the data pager
    }
}