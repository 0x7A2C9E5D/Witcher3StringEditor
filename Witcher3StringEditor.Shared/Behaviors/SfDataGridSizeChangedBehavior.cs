using System.Windows;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that handles grid resizing operations
///     Resets the row height manager and invalidates the visual container once a resize completed
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
    ///     The reset is coalesced onto a single dispatcher callback because SizeChanged fires once per layout
    ///     pass while a window or splitter is being dragged
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">SizeChanged event arguments containing information about the size change</param>
    private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _ = AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background, ResetLayout);
    }

    /// <summary>
    ///     Resets the row height manager and invalidates the visual container measurement
    /// </summary>
    private void ResetLayout()
    {
        var visualContainer = AssociatedObject.GetVisualContainer();
        if (visualContainer is null) return;
        visualContainer.RowHeightManager.Reset();
        visualContainer.InvalidateMeasureInfo();
    }
}