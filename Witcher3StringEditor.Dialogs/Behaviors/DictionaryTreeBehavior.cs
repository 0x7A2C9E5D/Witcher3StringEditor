using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     A behavior class for handling selection changes in a TreeView
/// </summary>
internal class DictionaryTreeBehavior : Behavior<TreeView>
{
    /// <summary>
    ///     Dependency property for storing the selected item
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), 
            typeof(DictionaryTreeBehavior), new PropertyMetadata(null));

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
    ///     Registers the event handler and seeds the current selection
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.SelectedItemChanged += AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters the event handler and clears the residual selection
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.SelectedItemChanged -= AssociatedObject_SelectedItemChanged;
    }

    /// <summary>
    ///     Handles the SelectedItemChanged event of the TreeView
    ///     Updates the SelectedItem property when a dictionary is selected
    /// </summary>
    private void AssociatedObject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is DictionaryInfo dictionaryInfo) SelectedItem = dictionaryInfo;
    }
}