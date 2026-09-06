namespace Witcher3StringEditor.Dialogs.Views;

/// <summary>
///     Interaction logic for DeleteDataDialog.xaml
///     This dialog allows users to confirm deletion of selected The Witcher 3 string items
///     Displays the items to be deleted in a data grid for review before deletion
/// </summary>
public partial class DeleteDataDialog
{
    /// <summary>
    ///     Initializes a new instance of the DeleteDataDialog class.
    ///     Grid disposal is handled through an attached behavior.
    /// </summary>
    public DeleteDataDialog()
    {
        InitializeComponent();
    }
}