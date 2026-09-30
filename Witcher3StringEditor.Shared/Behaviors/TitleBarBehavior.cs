using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using Microsoft.Xaml.Behaviors;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     A behavior for windows that use a custom title bar (extended view into the title bar)
///     Keeps the window content clear of the system caption buttons by resizing the column that
///     is named <c>RightPaddingColumn</c> to the system overlay right inset
/// </summary>
public sealed class TitleBarBehavior : Behavior<Window>
{
    /// <summary>
    ///     The name of the column that is reserved for the system caption buttons
    /// </summary>
    private const string RightPaddingColumnName = "RightPaddingColumn";

    /// <summary>
    ///     Called when the behavior is attached to the associated window
    ///     Registers the Loaded and SizeChanged handlers used to lay out the title bar
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the associated window
    ///     Unregisters the Loaded and SizeChanged handlers
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
    }

    /// <summary>
    ///     Sets the custom title bar regions once the window has loaded
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments</param>
    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        SetRegionsForCustomTitleBar();
    }

    /// <summary>
    ///     Updates the custom title bar regions when the window size changes
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments containing size change information</param>
    private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetRegionsForCustomTitleBar();
    }

    /// <summary>
    ///     Sets regions for custom title bar
    ///     Adjusts the right padding column width based on system overlay inset
    /// </summary>
    private void SetRegionsForCustomTitleBar()
    {
        if (!TitleBar.GetExtendViewIntoTitleBar(AssociatedObject)) return;
        if (AssociatedObject.FindName(RightPaddingColumnName) is not ColumnDefinition paddingColumn) return;
        paddingColumn.Width = new GridLength(TitleBar.GetSystemOverlayRightInset(AssociatedObject));
    }
}