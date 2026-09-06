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