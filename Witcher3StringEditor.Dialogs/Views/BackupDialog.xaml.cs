namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for BackupDialog.xaml
///     This dialog displays backup files and allows users to restore or delete them
/// </summary>
public partial class BackupDialog
{
    /// <summary>
    ///     Initializes a new instance of the BackupDialog class
    ///     Sets up the UI components and search helper
    /// </summary>
    public BackupDialog()
    {
        InitializeComponent(); // InitializeComponent
    }

    /// <summary>
    ///     Handles the closed event of the backup dialog
    ///     Disposes of resources to prevent memory leaks
    /// </summary>
    /// <param name="sender">The object that triggered the event</param>
    /// <param name="e">The event arguments</param>
    private void BackupDialog_OnClosed(object? sender, EventArgs e)
    {
        SfDataGrid.SearchHelper.Dispose();
        SfDataGrid.Dispose();
        SfDataPager.Dispose();
    }
}