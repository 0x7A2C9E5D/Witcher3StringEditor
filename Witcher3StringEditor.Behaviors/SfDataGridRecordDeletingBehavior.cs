using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Xaml.Behaviors;
using Serilog;
using Syncfusion.UI.Xaml.Grid;

namespace Witcher3StringEditor.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that handles record deletion confirmation
///     Intercepts record deletion events and shows a confirmation dialog before allowing the deletion to proceed
/// </summary>
public class SfDataGridRecordDeletingBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers the RecordDeleting event handler
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.RecordDeleting += AssociatedObject_RecordDeleting;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the RecordDeleting event handler
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.RecordDeleting -= AssociatedObject_RecordDeleting;
    }

    /// <summary>
    ///     Handles the RecordDeleting event of the SfDataGrid
    ///     Shows a confirmation dialog before allowing record deletion to proceed
    ///     Uses the messenger pattern to communicate with view models for confirmation
    /// </summary>
    /// <param name="sender">The event sender (SfDataGrid instance)</param>
    /// <param name="e">RecordDeleting event arguments containing information about the record to be deleted</param>
    private static async void AssociatedObject_RecordDeleting(object? sender, RecordDeletingEventArgs e)
    {
        try
        {
            var recordItemType = e.Items[0].GetType().Name;
            if (!await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(), recordItemType))
                e.Cancel = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deleting a record.");
        }
    }
}