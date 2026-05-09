using System.Windows;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that handles grid resizing operations
///     Resets the row height manager and invalidates the visual container when the grid size changes
///     This ensures proper layout update when the grid is resized
/// </summary>
public class SfDataGridSizeChangedBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers the SizeChanged event handler
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the SizeChanged event handler
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
    }

    /// <summary>
    ///     Handles the SizeChanged event of the SfDataGrid
    ///     Resets the row height manager and invalidates the visual container measurement
    ///     This ensures that the grid properly updates its layout when its size changes
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">SizeChanged event arguments containing information about the size change</param>
    private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AssociatedObject.GetVisualContainer().RowHeightManager.Reset();
        AssociatedObject.GetVisualContainer().InvalidateMeasureInfo();
    }
}