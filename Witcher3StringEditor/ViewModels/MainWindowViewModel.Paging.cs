using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Witcher3StringEditor.Messaging;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Paging and search: the source the data grid currently shows.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Gets or sets the source collection for the DataGrid
    /// </summary>
    [ObservableProperty]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "The interface type is intentional: the fallback shares the live W3StringItems instance.")]
    // Resharper disable once MemberCanBeMadeStatic.Local
    private partial IList<W3StringItem>? PagedSource { get; set; }

    /// <summary>
    ///     Gets or sets the current search text used for filtering W3String items
    /// </summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>
    ///     Requests an updated paged source for the data grid
    /// </summary>
    private async Task RequestDataGridPagedSource()
    {
        await Task.Delay(100); // Delay to allow time for the collection to update
        PagedSource = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<List<W3StringItem>>(),
            MessageTokens.RequestDataGridPagedSource); // Send request for updated paged source
    }
}