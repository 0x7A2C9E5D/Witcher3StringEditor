using iNKORE.UI.WPF.Modern.Controls;

namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for LogDialog.xaml
///     This dialog displays application logs in a data grid with search and pagination capabilities
/// </summary>
public partial class LogDialog
{
    /// <summary>
    ///     Initializes a new instance of the LogDialog class
    ///     Sets up the UI components and configures the search helper
    /// </summary>
    public LogDialog()
    {
        InitializeComponent();
        SetupSearchHelper();
    }

    /// <summary>
    ///     Sets up the search helper for the data grid
    ///     Enables filtering and disables case-sensitive search
    /// </summary>
    private void SetupSearchHelper()
    {
        SfDataGrid.SearchHelper.AllowFiltering = true; // Enable filtering
        SfDataGrid.SearchHelper.AllowCaseSensitiveSearch = false; // Disable case-sensitive search
        SfDataGrid.SearchHelper.CanHighlightSearchText = false; // Disable highlighting of search text
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
    ///     Handles the closed event of the log dialog
    ///     Disposes of resources to prevent memory leaks when the dialog is closed
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void LogDialog_OnClosed(object? sender, EventArgs e)
    {
        SfDataGrid.SearchHelper.Dispose();
        SfDataGrid.Dispose();
        SfDataPager.Dispose();
    }
}