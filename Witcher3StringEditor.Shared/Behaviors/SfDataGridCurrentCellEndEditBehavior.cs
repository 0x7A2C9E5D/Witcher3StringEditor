using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Witcher3StringEditor.Shared.Behaviors;

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
    ///     Invalidates the row height of the edited row so the grid can re-measure content whose height changed
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">CurrentCellEndEdit event arguments containing information about the edited cell</param>
    private void AssociatedObject_CurrentCellEndEdit(object? sender, CurrentCellEndEditEventArgs e)
    {
        // Syncfusion uses -1 when no row could be resolved; the invalidation would be meaningless there
        var rowIndex = e.RowColumnIndex.RowIndex;
        if (rowIndex < 0) return;
        AssociatedObject.InvalidateRowHeight(rowIndex);
        // InvalidateRowHeight already schedules the row layout update; the container is only invalidated when it
        // is actually available, so the whole grid is not re-measured on every edit commit
        AssociatedObject.GetVisualContainer()?.InvalidateMeasureInfo();
    }
}