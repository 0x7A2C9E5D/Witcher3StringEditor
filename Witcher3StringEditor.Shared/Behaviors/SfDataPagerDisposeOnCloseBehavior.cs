using System.Windows;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Controls.DataPager;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataPager that releases pager resources once the
///     pager is unloaded (e.g. when the hosting dialog closes). Shared by the backup,
///     log and recent files dialogs.
/// </summary>
public sealed class SfDataPagerDisposeOnCloseBehavior : Behavior<SfDataPager>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject.
    ///     Subscribes to the pager Unloaded event.
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject.
    ///     Unsubscribes from the pager Unloaded event.
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
    }

    /// <summary>
    ///     Releases the pager resources once the pager is unloaded.
    /// </summary>
    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Dispose();
    }
}