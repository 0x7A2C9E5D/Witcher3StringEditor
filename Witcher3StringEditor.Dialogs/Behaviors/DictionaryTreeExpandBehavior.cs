using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;
using Witcher3StringEditor.Dialogs.Models;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     An attached behavior for TreeView that maintains expansion state of groups across data updates
///     Uses simple string (culture name) as key for expanded groups
/// </summary>
internal class DictionaryTreeExpandBehavior : Behavior<TreeView>
{
    /// <summary>
    ///     Dependency property for storing the expanded groups
    ///     Uses culture name as simple string key
    /// </summary>
    private static readonly DependencyProperty ExpandedGroupsProperty =
        DependencyProperty.Register("ExpandedGroups", typeof(HashSet<string>), typeof(DictionaryTreeExpandBehavior),
            new PropertyMetadata(null));

    /// <summary>
    ///     Gets the set of expanded groups by culture name
    /// </summary>
    private HashSet<string> ExpandedGroups
    {
        get
        {
            var value = GetValue(ExpandedGroupsProperty);
            if (value != null) return (HashSet<string>)value;
            value = new HashSet<string>();
            SetValue(ExpandedGroupsProperty, value);
            return (HashSet<string>)value;
        }
    }

    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject
    ///     Registers necessary event handlers
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnTreeViewLoaded;
        AssociatedObject.ItemContainerGenerator.StatusChanged += OnItemContainerStatusChanged;
        AssociatedObject.ItemContainerGenerator.ItemsChanged += OnItemsChanged;
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject
    ///     Unregisters event handlers
    /// </summary>
    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnTreeViewLoaded;
        AssociatedObject.ItemContainerGenerator.StatusChanged -= OnItemContainerStatusChanged;
        AssociatedObject.ItemContainerGenerator.ItemsChanged -= OnItemsChanged;
    }

    /// <summary>
    ///     Handles the Loaded event of the TreeView
    ///     Restores expansion state for groups that were previously expanded
    /// </summary>
    private void OnTreeViewLoaded(object sender, RoutedEventArgs e)
    {
        RestoreExpansionState();
    }

    /// <summary>
    ///     Handles the ItemsChanged event of the ItemContainerGenerator
    ///     This is called when items are added or removed
    /// </summary>
    private void OnItemsChanged(object? sender, ItemsChangedEventArgs e)
    {
        // When new items are generated, subscribe to their expanded/collapsed events
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        foreach (var item in AssociatedObject.Items)
            if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                SubscribeToItemEvents(treeViewItem);
    }

    /// <summary>
    ///     Handles the StatusChanged event of the ItemContainerGenerator
    ///     This is called when items are regenerated (e.g. after ReGroup)
    /// </summary>
    private void OnItemContainerStatusChanged(object? sender, EventArgs e)
    {
        if (AssociatedObject.ItemContainerGenerator.Status !=
            GeneratorStatus.ContainersGenerated) return;
        // After containers are generated, restore the expansion state and subscribe to events
        RestoreExpansionState();
        SubscribeToAllItemEvents();
    }

    /// <summary>
    ///     Subscribes to the Expanded and Collapsed events of all TreeViewItems
    /// </summary>
    private void SubscribeToAllItemEvents()
    {
        foreach (var item in AssociatedObject.Items)
            if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                SubscribeToItemEvents(treeViewItem);
    }

    /// <summary>
    ///     Subscribes to the Expanded and Collapsed events of a specific TreeViewItem
    /// </summary>
    private void SubscribeToItemEvents(TreeViewItem item)
    {
        if (item.DataContext is not DictionaryGroup) return;
        // Remove existing handlers to avoid duplicates
        item.Expanded -= OnTreeViewItemExpanded;
        item.Collapsed -= OnTreeViewItemCollapsed;

        // Add new handlers
        item.Expanded += OnTreeViewItemExpanded;
        item.Collapsed += OnTreeViewItemCollapsed;
    }

    /// <summary>
    ///     Handles the Expanded event of a TreeViewItem
    ///     Adds the group's culture name to expanded state
    /// </summary>
    private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: DictionaryGroup group })
            ExpandedGroups.Add(group.TargetLanguage.Name);
    }

    /// <summary>
    ///     Handles the Collapsed event of a TreeViewItem
    ///     Removes the group's culture name from expanded state
    /// </summary>
    private void OnTreeViewItemCollapsed(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: DictionaryGroup group })
            ExpandedGroups.Remove(group.TargetLanguage.Name);
    }

    /// <summary>
    ///     Restores the expansion state for groups that were previously expanded
    /// </summary>
    private void RestoreExpansionState()
    {
        foreach (var item in AssociatedObject.Items)
        {
            if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem treeViewItem ||
                item is not DictionaryGroup group) continue;
            // Check if this group was previously expanded by comparing culture names
            if (ExpandedGroups.Contains(group.TargetLanguage.Name)) treeViewItem.IsExpanded = true;
        }
    }
}