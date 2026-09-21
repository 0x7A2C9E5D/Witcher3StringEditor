using System.Windows;
using Microsoft.Xaml.Behaviors;
using Serilog;
using Syncfusion.UI.Xaml.Grid;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that releases the grid search helper and grid resources once the
///     grid is unloaded (e.g. when the hosting dialog closes)
///     Shared by the backup, delete data, log and recent files dialogs
/// </summary>
public sealed class SfDataGridDisposeOnCloseBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Subscribes to the grid Unloaded event
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unsubscribes from the grid Unloaded event
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Releases the search helper and the grid resources once the grid is unloaded
    ///     is not a one-shot event in WPF, so the release is guarded and the subscription removed first
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">The event arguments</param>
    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            AssociatedObject.SearchHelper?.Dispose();
            AssociatedObject.Dispose();
        }
        catch (Exception ex)
        {
            // An exception while a dialog closes would otherwise propagate on the UI thread and crash the app
            Log.Error(ex, "Failed to release the data grid resources");
        }
    }
}