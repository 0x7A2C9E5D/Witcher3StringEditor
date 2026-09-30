using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Xaml.Behaviors;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     A behavior that cleans up the associated window when it is closed.
///     Unregisters the messenger handlers of the window itself and of its DataContext
///     (typically the view model), so message registrations do not outlive the window.
/// </summary>
public sealed class WindowCloseBehavior : Behavior<Window>
{
    /// <summary>
    ///     Called when the behavior is attached to the associated window.
    ///     Subscribes to the window Closed event.
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Closed += AssociatedObject_Closed;
    }

    /// <summary>
    ///     Called when the behavior is detached from the associated window.
    ///     Unsubscribes from the window Closed event.
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Closed -= AssociatedObject_Closed;
    }

    /// <summary>
    ///     Unregisters all messenger handlers for the window and its DataContext.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments</param>
    private void AssociatedObject_Closed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(AssociatedObject); // Unregister the window handlers
        if (AssociatedObject.DataContext is { } dataContext && !ReferenceEquals(dataContext, AssociatedObject))
            WeakReferenceMessenger.Default.UnregisterAll(dataContext); // Unregister the DataContext handlers
    }
}