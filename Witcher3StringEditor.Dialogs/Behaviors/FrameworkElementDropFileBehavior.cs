using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     An attached behavior that adds file drag-and-drop support to a FrameworkElement
/// </summary>
public class FrameworkElementDropFileBehavior : Behavior<FrameworkElement>
{
    /// <summary>
    ///     Data dependency property for storing the array of dropped file paths
    /// </summary>
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data),
            typeof(string[]),
            typeof(FrameworkElementDropFileBehavior),
            new PropertyMetadata(null));

    /// <summary>
    ///     Gets or sets the array of dropped file paths
    /// </summary>
    public string[]? Data
    {
        get => (string[])GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers drag event handlers
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.DragEnter += AssociatedObject_DragEnterOrDragLeave;
        AssociatedObject.DragLeave += AssociatedObject_DragEnterOrDragLeave;
        AssociatedObject.Drop += AssociatedObject_Drop;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters drag event handlers
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.DragEnter -= AssociatedObject_DragEnterOrDragLeave;
        AssociatedObject.DragLeave -= AssociatedObject_DragEnterOrDragLeave;
        AssociatedObject.Drop -= AssociatedObject_Drop;
    }

    /// <summary>
    ///     Handles DragEnter and DragLeave events
    ///     Sets the drag effect: Copy if data contains files, None otherwise
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">Drag event arguments</param>
    private static void AssociatedObject_DragEnterOrDragLeave(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    ///     Handles the Drop event
    ///     Extracts file paths from the drag data and sets them to the Data property
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">Drag event arguments</param>
    private void AssociatedObject_Drop(object sender, DragEventArgs e)
    {
        Data = (string[]?)e.Data.GetData(DataFormats.FileDrop);
    }
}