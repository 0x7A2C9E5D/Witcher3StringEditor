using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;
using Witcher3StringEditor.Dialogs.Models;
using Witcher3StringEditor.Dictionary;

namespace Witcher3StringEditor.Dialogs.Behaviors;

/// <summary>
///     A behavior attached to TreeView for maintaining group expansion state and handling selection changes
///     Uses simple string (culture name) as key for expanded groups
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
    ///     Stores expanded groups (using culture name as key)
    /// </summary>
    private readonly HashSet<CultureInfo> expandedGroups = [];

    /// <summary>
    ///     Stores the previously selected item
    /// </summary>
    private DictionaryInfo? previousSelectedItem;

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
        AssociatedObject.Loaded += OnTreeViewLoaded;
        AssociatedObject.SelectedItemChanged += AssociatedObject_SelectedItemChanged;
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
        AssociatedObject.SelectedItemChanged -= AssociatedObject_SelectedItemChanged;
        AssociatedObject.ItemContainerGenerator.StatusChanged -= OnItemContainerStatusChanged;
        AssociatedObject.ItemContainerGenerator.ItemsChanged -= OnItemsChanged;
    }

    /// <summary>
    ///     Handles the Loaded event of the TreeView
    ///     Restores expansion state for previously expanded groups
    /// </summary>
    private void OnTreeViewLoaded(object sender, RoutedEventArgs e)
    {
        RestoreExpansionState();
    }

    /// <summary>
    ///     Handles the ItemsChanged event of the ItemContainerGenerator
    ///     Called when items are added or removed
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
    ///     Called when items are regenerated (e.g. after ReGroup)
    /// </summary>
    private void OnItemContainerStatusChanged(object? sender, EventArgs e)
    {
        if (AssociatedObject.ItemContainerGenerator.Status !=
            GeneratorStatus.ContainersGenerated) return;
        // After containers are generated, restore expansion state and subscribe to events
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
    ///     Adds the group's culture name to expansion state
    /// </summary>
    private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: DictionaryGroup group })
            expandedGroups.Add(group.TargetLanguage);
    }

    /// <summary>
    ///     Handles the Collapsed event of a TreeViewItem
    ///     Removes the group's culture name from expansion state
    /// </summary>
    private void OnTreeViewItemCollapsed(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: DictionaryGroup group })
            expandedGroups.Remove(group.TargetLanguage);
    }

    /// <summary>
    ///     Restores the expansion state for previously expanded groups
    /// </summary>
    private void RestoreExpansionState()
    {
        if (expandedGroups.Count == 0) return;

        TreeViewItem? toSelect = null;

        // Get only groups that need to be expanded (intersection of data source and expanded state)
        var groupsToExpand = AssociatedObject.ItemsSource.OfType<DictionaryGroup>()
            .Where(group => expandedGroups.Contains(group.TargetLanguage)).ToList();
        
        foreach (var group in groupsToExpand)
        {
            if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(group) is not TreeViewItem treeViewItem)
                continue;

            treeViewItem.IsExpanded = true;
            treeViewItem.UpdateLayout();

            // Skip selection logic if we already found the item to select or no previous selection exists
            if (previousSelectedItem == null || toSelect != null)
                continue;

            var dictionaryInfo = group.Dictionaries.FirstOrDefault(previousSelectedItem);
            if (dictionaryInfo == null) continue;

            toSelect = treeViewItem.ItemContainerGenerator.ContainerFromItem(dictionaryInfo) as TreeViewItem;
            if (toSelect != null)
                toSelect.IsSelected = true;
        }
        
    }

    /// <summary>
    ///     Handles the SelectedItemChanged event of the TreeView
    ///     Updates the SelectedItem property if the selected value is of type DictionaryInfo
    /// </summary>
    private void AssociatedObject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        previousSelectedItem = e.OldValue as DictionaryInfo ?? previousSelectedItem;
        SelectedItem = e.NewValue is DictionaryInfo ? e.NewValue : null;
    }
}