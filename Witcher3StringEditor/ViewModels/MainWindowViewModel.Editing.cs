using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Models;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Editing commands: adding, changing, deleting The Witcher 3 string items and merging another file's texts
///     into them.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Shows the edit dialog for a new item and adds it to the collection when the user confirms
    /// </summary>
    /// <remarks>
    ///     The new item starts from the ID suggested by <see cref="CreateNextStrId" />, so that adding an
    ///     item does not begin with an empty ID field. That ID is written into the working copy of the
    ///     dialog after it was created, because the dialog tells adding and editing apart by whether the
    ///     item it is given already carries an ID
    /// </remarks>
    private async Task AddNewItem()
    {
        var dialogViewModel = dialogViewModelFactory.CreateEditDialog(new W3StringItem()); // Create new item view model
        if (dialogViewModel.Item is { } newItem) // Check that the dialog holds a working copy
            newItem.StrId = CreateNextStrId(); // Start from the suggested ID
        if (await dialogService.ShowDialogAsync(this, dialogViewModel) == true // Show add dialog
            && dialogViewModel.Item is not null) // Check if user confirmed
        {
            W3StringItems!.Add((W3StringItem)dialogViewModel.Item); // Add new item to collection
            await RequestDataGridPagedSource(); // Request updated paged source
            Log.Information("W3String item added: {StrId}", dialogViewModel.Item.StrId); // Log successful addition
        }
    }

    /// <summary>
    ///     Creates the string ID a new item is pre-filled with
    /// </summary>
    /// <returns>
    ///     The number above the highest numeric ID of the loaded file, or an empty string when the
    ///     file holds no numeric ID or the highest one is already <see cref="uint.MaxValue" />
    /// </returns>
    /// <remarks>
    ///     The highest ID is taken from every loaded item, not from the current page or search
    ///     result, because a repeated ID is only rejected when the file is written
    /// </remarks>
    private string CreateNextStrId()
    {
        if (W3StringItems is null) return string.Empty; // Nothing the suggestion could continue from
        var hasNumericId = false; // Whether a numeric ID has been seen
        var highest = 0u; // The highest numeric ID seen so far
        foreach (var item in W3StringItems)
        {
            // Parse like the writer does, so the suggestion follows the IDs the file may hold
            if (!uint.TryParse(item.StrId, out var id)) continue; // Ignore an ID that is not a number
            if (hasNumericId && id <= highest) continue; // Ignore an ID below the current highest
            highest = id; // Remember the new highest ID
            hasNumericId = true; // Remember that the file holds a numeric ID
        }

        return hasNumericId && highest < uint.MaxValue // A next ID only exists below the maximum
            ? (highest + 1).ToString(CultureInfo.InvariantCulture) // Suggest the next free number
            : string.Empty; // Leave the ID empty when there is nothing to continue from
    }

    /// <summary>
    ///     Edits the selected The Witcher 3 string item
    /// </summary>
    /// <param name="selectedItem">The item to edit, or null when no data row is selected</param>
    /// <remarks>
    ///     Without a selected item this command adds a new item instead, so that the edit button
    ///     stays usable while the data grid holds no selection
    /// </remarks>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task Edit(W3StringItem? selectedItem)
    {
        if (selectedItem is null) await AddNewItem(); // Add a new item when no data row is selected
        else await EditItem(selectedItem); // Otherwise edit the selected item
    }

    /// <summary>
    ///     Shows the edit dialog for an existing item and copies the confirmed values onto it
    /// </summary>
    /// <param name="selectedItem">The item to edit</param>
    private async Task EditItem(W3StringItem selectedItem)
    {
        var dialogViewModel = dialogViewModelFactory.CreateEditDialog(selectedItem); // Create edit dialog view model
        if (await dialogService.ShowDialogAsync(this, // Show edit dialog
                dialogViewModel) == true && dialogViewModel.Item is not null) // Check if user confirmed changes
        {
            var found = W3StringItems! // Find the item in the collection
                .First(x => x.TrackingId == selectedItem.TrackingId);
            var index = W3StringItems!.IndexOf(found); // Get the item index
            // Update the item properties
            W3StringItems[index].StrId = dialogViewModel.Item.StrId;
            W3StringItems[index].KeyHex = dialogViewModel.Item.KeyHex;
            W3StringItems[index].KeyName = dialogViewModel.Item.KeyName;
            W3StringItems[index].Text = dialogViewModel.Item.Text;
            Log.Information("W3String item updated: {StrId}", dialogViewModel.Item.StrId); // Log successful update
        }
    }

    /// <summary>
    ///     Deletes the selected The Witcher 3 string items
    /// </summary>
    /// <param name="selectedItems">The items to delete</param>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task Delete(IEnumerable<object> selectedItems)
    {
        var w3Items = selectedItems
            .OfType<ITrackableW3StringItem>().ToArray(); // Filter and convert to trackable items
        if (w3Items.Length > 0 &&
            await dialogService.ShowDialogAsync(this, dialogViewModelFactory.CreateDeleteDialog(w3Items)) ==
            true) // Show delete confirmation dialog
        {
            foreach (var item in w3Items)
            {
                var stringItem = (W3StringItem)item; // Cast to string item model
                W3StringItems!.Remove(stringItem); // Remove from main collection
            }

            Log.Information("Deleted {Count} W3String item(s)", w3Items.Length);
            await RequestDataGridPagedSource(); // Request updated paged source
        }
    }

    /// <summary>
    ///     Merges data from a file
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasW3StringItems))]
    private async Task MergeData()
    {
        try
        {
            // Show open file dialog
            using var storageFile =
                await dialogService.ShowOpenFileDialogAsync(this, CreateOpenFileDialogSettings());
            if (storageFile is not null &&
                IsSupportedFile(storageFile.LocalPath) &&
                await dialogService.MessageBoxConfirmAsync(this, Strings.MergeDataConfirmMessage,
                    Strings.MergeDataConfirmCaption)) // Ask before overwriting the loaded data
            {
                // Deserialize the file
                var mergeData =
                    await iw3StringSerializer.Deserialize(storageFile.LocalPath);

                // Join the two collections
                var matchedPairs = W3StringItems!.Join(
                    mergeData,
                    oldItem => oldItem.StrId,
                    newItem => newItem.StrId,
                    (oldItem, newItem) => new { oldItem, newItem }
                ).ToArray();

                // Update the text of the old items
                foreach (var pair in matchedPairs)
                    pair.oldItem.Text = pair.newItem.Text;

                Log.Information("Merged {MatchedCount} item(s) from {SourceFile}",
                    matchedPairs.Length, storageFile.LocalPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to merge data");
        }
    }
}