using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that handles row height invalidation when a cell edit operation ends
/// </summary>
public class SfDataGridCurrentCellEndEditBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers the CurrentCellEndEdit event handler
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.CurrentCellEndEdit += AssociatedObject_CurrentCellEndEdit;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the CurrentCellEndEdit event handler
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.CurrentCellEndEdit -= AssociatedObject_CurrentCellEndEdit;
    }

    /// <summary>
    ///     Handles the CurrentCellEndEdit event of the SfDataGrid
    ///     Invalidates the row height and visual container measurement when a cell editing operation ends
    ///     This ensures the grid properly updates its layout after editing content that might affect row height
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">CurrentCellEndEdit event arguments containing information about the edited cell</param>
    private void AssociatedObject_CurrentCellEndEdit(object? sender, CurrentCellEndEditEventArgs e)
    {
        AssociatedObject.InvalidateRowHeight(e.RowColumnIndex.RowIndex);
        AssociatedObject.GetVisualContainer().InvalidateMeasureInfo();
    }
}