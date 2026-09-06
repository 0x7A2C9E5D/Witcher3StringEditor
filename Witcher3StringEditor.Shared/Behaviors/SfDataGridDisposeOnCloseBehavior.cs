using System.Windows;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that releases the grid search helper and
///     grid resources once the grid is unloaded (e.g. when the hosting dialog closes).
///     Shared by the backup, delete data, log and recent files dialogs.
/// </summary>
public sealed class SfDataGridDisposeOnCloseBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject.
    ///     Subscribes to the grid Unloaded event.
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject.
    ///     Unsubscribes from the grid Unloaded event.
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Releases the search helper and the grid resources once the grid is unloaded.
    /// </summary>
    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.SearchHelper.Dispose();
        AssociatedObject.Dispose();
    }
}