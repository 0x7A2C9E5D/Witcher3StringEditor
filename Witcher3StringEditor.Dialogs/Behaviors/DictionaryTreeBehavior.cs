using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///    A behavior class for handling selection changes in a TreeView
/// </summary>
internal class DictionaryTreeBehavior : Behavior<TreeView>
{
    /// <summary>
    ///     Dependency property for storing the selected item
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DictionaryTreeBehavior),
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
    ///     Registers necessary event handlers
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.SelectedItemChanged += AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters event handlers
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.SelectedItemChanged -= AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Handles the SelectedItemChanged event of the TreeView
    ///     Updates the SelectedItem property if the selected value is of type DictionaryInfo
    /// </summary>
    private void AssociatedObject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        SelectedItem = e.NewValue is DictionaryInfo ? e.NewValue : null;
    }
}