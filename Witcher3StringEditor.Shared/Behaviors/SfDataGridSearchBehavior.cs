using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;

namespace Witcher3StringEditor.Shared.Behaviors;

/// <summary>
///     An attached behavior for SfDataGrid that configures its search helper and wires
///     an AutoSuggestBox to perform and clear searches. Shared by the backup, log,
///     recent files and dictionary manager dialogs.
/// </summary>
public class SfDataGridSearchBehavior : Behavior<SfDataGrid>
{
    /// <summary>
    ///     Dependency property for the search box whose query is applied to the grid.
    /// </summary>
    public static readonly DependencyProperty SearchBoxProperty = DependencyProperty.Register(
        nameof(SearchBox), typeof(AutoSuggestBox), typeof(SfDataGridSearchBehavior),
        new PropertyMetadata(null, OnSearchBoxChanged));

    private AutoSuggestBox? attachedSearchBox;

    /// <summary>
    ///     Gets or sets the AutoSuggestBox that drives the grid search.
    /// </summary>
    public AutoSuggestBox? SearchBox
    {
        get => (AutoSuggestBox?)GetValue(SearchBoxProperty);
        set => SetValue(SearchBoxProperty, value);
    }

    /// <summary>
    ///     Called when the behavior is attached to the AssociatedObject.
    ///     Configures the search helper and subscribes to the search box events.
    /// </summary>
    protected override void OnAttached()
    {
        AssociatedObject.SearchHelper.AllowFiltering = true;
        AssociatedObject.SearchHelper.AllowCaseSensitiveSearch = false;
        AssociatedObject.SearchHelper.CanHighlightSearchText = false;
        AttachSearchBox(SearchBox);
    }

    /// <summary>
    ///     Called when the behavior is detached from the AssociatedObject.
    ///     Unsubscribes from the search box events.
    /// </summary>
    protected override void OnDetaching()
    {
        AttachSearchBox(null);
    }

    /// <summary>
    ///     Handles changes of the SearchBox dependency property.
    /// </summary>
    private static void OnSearchBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SfDataGridSearchBehavior)d).AttachSearchBox((AutoSuggestBox?)e.NewValue);
    }

    /// <summary>
    ///     Swaps the event subscription to the given search box, if any.
    /// </summary>
    /// <param name="searchBox">The search box to subscribe to, or null to unsubscribe.</param>
    private void AttachSearchBox(AutoSuggestBox? searchBox)
    {
        if (ReferenceEquals(attachedSearchBox, searchBox)) return;
        if (attachedSearchBox is not null)
        {
            attachedSearchBox.QuerySubmitted -= SearchBox_OnQuerySubmitted;
            attachedSearchBox.TextChanged -= SearchBox_OnTextChanged;
        }

        attachedSearchBox = searchBox;
        if (searchBox is null) return;
        searchBox.QuerySubmitted += SearchBox_OnQuerySubmitted;
        searchBox.TextChanged += SearchBox_OnTextChanged;
    }

    /// <summary>
    ///     Performs a search in the data grid when a non-empty query is submitted.
    /// </summary>
    private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.QueryText)) return;
        AssociatedObject.SearchHelper.Search(args.QueryText);
    }

    /// <summary>
    ///     Clears the grid search when the search box text becomes empty.
    /// </summary>
    private void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(sender.Text))
            AssociatedObject.SearchHelper.ClearSearch();
    }
}