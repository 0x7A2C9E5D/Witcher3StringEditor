using System.Windows;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that handles dynamic row height calculation based on content
/// </summary>
public class SfDataGridQueryRowHeightBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Dependency property for minimum row height, with a default value of 25.0
    /// </summary>
    public static readonly DependencyProperty MinHeightProperty
        = DependencyProperty.Register(nameof(MinHeight), typeof(double), 
            typeof(SfDataGridQueryRowHeightBehavior), new PropertyMetadata(25.0));

    /// <summary>
    ///     Grid row sizing options used for calculating auto row heights
    /// </summary>
    private readonly GridRowSizingOptions gridRowResizingOptions = new();

    /// <summary>
    ///     Gets or sets the minimum height for grid rows
    /// </summary>
    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers the QueryRowHeight event handler
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.QueryRowHeight += AssociatedObject_QueryRowHeight;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the QueryRowHeight event handler
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.QueryRowHeight -= AssociatedObject_QueryRowHeight;
    }

    /// <summary>
    ///     Handles the QueryRowHeight event of the SfDataGrid
    ///     Calculates the appropriate row height based on the content and clamps it to <see cref="MinHeight" />
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">QueryRowHeight event arguments containing row index and height information</param>
    private void AssociatedObject_QueryRowHeight(object? sender, QueryRowHeightEventArgs e)
    {
        // Only bail out when the auto sizing failed; the minimum has to be clamped explicitly, otherwise a row
        // falls back to the grid default and could end up shorter than the requested minimum
        if (!AssociatedObject.GridColumnSizer.GetAutoRowHeight(e.RowIndex, gridRowResizingOptions,
                out var autoHeight)) return;
        e.Height = Math.Max(autoHeight, MinHeight);
        e.Handled = true;
    }
}