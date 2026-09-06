using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using Serilog;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    /// <summary>
    ///     Initializes a new instance of the MainWindow class
    ///     Sets up the main window components, data grid, and message handlers
    /// </summary>
    public MainWindow()
    {
        InitializeComponent(); // Initialize the UI components
        SetupSearchHelper(); // Set up the search helper functionality
        RegisterMessageHandlers(); // Register message handlers for inter-component communication
    }

    /// <summary>
    ///     Sets up the search helper for the data grid
    ///     Configures filtering and case sensitivity options
    /// </summary>
    private void SetupSearchHelper()
    {
        SfDataGrid.SearchHelper.AllowFiltering = true; // Enable filtering
        SfDataGrid.SearchHelper.AllowCaseSensitiveSearch = false; // Disable case-sensitive search
        SfDataGrid.SearchHelper.CanHighlightSearchText = false; // Disable highlighting search text
    }

    /// <summary>
    ///     Registers all message handlers for the main window
    /// </summary>
    private void RegisterMessageHandlers()
    {
        RegisterDataGridSourceHandler(); // Register the data grid paged source handler
        RegisterPageSizeChangedHandler(); // Register page size change message handler
    }

    /// <summary>
    ///     Registers message handler for page size change notifications
    ///     Updates the data pager's page size when PageSizeChanged message is received
    /// </summary>
    private void RegisterPageSizeChangedHandler()
    {
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<int>, string>(this, MessageTokens.PageSizeChanged,
            (_, m) =>
            {
                SfDataPager.PageSize = m.Value; // Update the data pager's page size
                Log.Debug("Page size changed to {PageSize}", m.Value); // Log the new page size
            });
    }

    /// <summary>
    ///     Registers the handler replying with the current data grid paged source
    /// </summary>
    private void RegisterDataGridSourceHandler()
    {
        WeakReferenceMessenger.Default.Register<MainWindow, AsyncRequestMessage<List<W3StringItem>>, string>(
            this,
            MessageTokens.RequestDataGridPagedSource,
            (_, m) =>
            {
                // Reply with the current data grid paged source
                m.Reply([
                    .. ((PagedCollectionView)SfDataGrid.ItemsSource)
                    .GetInternalList()
                    .Cast<W3StringItem>()
                ]);
            }); // Request data grid paged source
    }

    /// <summary>
    ///     Handles the QuerySubmitted event of the search box
    ///     Performs a search in the data grid and sends the results
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="args">The event arguments containing the query text</param>
    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            if (SfDataGrid.ItemsSource is null || string.IsNullOrWhiteSpace(args.QueryText))
                return; // Ensure there's data to search before proceeding
            SfDataGrid.SearchHelper.Search(args.QueryText); // Perform the search and collect results
            await NotifyDataGridSourceChanged(); // Notify data grid source changed
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error performing search.");
        }
    }

    /// <summary>
    ///     Handles the TextChanged event of the search box
    ///     Clears the search when the text is empty
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="args">The event arguments</param>
    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(sender.Text)) return; // Return if search text is not empty
            SfDataGrid.SearchHelper.ClearSearch(); // Clear the search helper results
            await NotifyDataGridSourceChanged(); // Notify data grid source changed
        }
        catch (Exception e)
        {
            Log.Error(e, "Error clearing search."); // Log the error
        }
    }

    /// <summary>
    ///     Handles the Closed event of the window
    ///     Unregisters message handlers and disposes resources
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments</param>
    private void Window_Closed(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this); // Unregister all message handlers
        SfDataGrid.SearchHelper.Dispose(); // Dispose the search helper
        SfDataGrid.Dispose(); // Dispose the data grid
        SfDataPager.Dispose(); // Dispose the data pager
    }

    /// <summary>
    ///     Handles the Loaded event of the app title bar
    ///     Sets up regions for custom title bar if extended view is enabled
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments</param>
    private void AppTitleBar_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (TitleBar.GetExtendViewIntoTitleBar(this)) SetRegionsForCustomTitleBar();
    }

    /// <summary>
    ///     Handles the SizeChanged event of the app title bar
    ///     Updates regions for custom title bar when size changes
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments containing size change information</param>
    private void AppTitleBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (TitleBar.GetExtendViewIntoTitleBar(this)) SetRegionsForCustomTitleBar();
    }

    /// <summary>
    ///     Sets regions for custom title bar
    ///     Adjusts the right padding column width based on system overlay inset
    /// </summary>
    private void SetRegionsForCustomTitleBar()
    {
        RightPaddingColumn.Width = new GridLength(TitleBar.GetSystemOverlayRightInset(this));
    }

    /// <summary>
    ///     Handles the Click event of the theme switch button
    ///     Toggles between light and dark themes
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments</param>
    private void ThemeSwitchBtn_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeManager.Current.ApplicationTheme =
            ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
    }

    /// <summary>
    ///     Handles the SortColumnsChanged event of the data grid
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SfDataGrid_OnSortColumnsChanged(object? sender, GridSortColumnsChangedEventArgs e)
    {
        try
        {
            await NotifyDataGridSourceChanged(); // Notify data grid source changed
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in SfDataGrid_OnSortColumnsChanged"); // Log the error
        }
    }

    /// <summary>
    ///     Notifies the data grid source changed
    /// </summary>
    private async Task NotifyDataGridSourceChanged()
    {
        await Task.Delay(100);
        var items = ((PagedCollectionView)SfDataGrid.ItemsSource)
            .GetInternalList().Cast<W3StringItem>()
            .ToList(); // Get the internal list and cast to W3StringItem
        WeakReferenceMessenger.Default.Send(items,
            MessageTokens.DataGridPagedSourceChanged); // Send the list to the message bus
    }
}