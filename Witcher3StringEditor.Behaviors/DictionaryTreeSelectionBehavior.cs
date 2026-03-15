using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Behaviors;

/// <summary>
///     An attached behavior for TreeView that handles selected item changes
/// </summary>
public class DictionaryTreeSelectionBehavior : Behavior<TreeView>
{
    /// <summary>
    ///     Dependency property for storing the selected item
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DictionaryTreeSelectionBehavior),
            new PropertyMetadata(null));

    /// <summary>
    ///     Gets or sets the selected item
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers the SelectedItemChanged event handler
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.SelectedItemChanged += AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the SelectedItemChanged event handler
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.SelectedItemChanged -= AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Handles the SelectedItemChanged event of the TreeView
    ///     Expands the selected item if it is a TreeViewItem
    /// </summary>
    private void AssociatedObject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is DictionaryInfo) 
            SelectedItem = e.NewValue;
    }
}