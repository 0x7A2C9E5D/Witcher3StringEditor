using System.Collections.ObjectModel;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides access to the recent files, backed by the replicated shared records state.
/// </summary>
/// <param name="sharedRecords">Shared records bridge.</param>
internal class RecentFilesService(ISharedRecordsState sharedRecords) : IRecentFilesService
{
    /// <summary>
    ///     Gets the collection of recent files.
    /// </summary>
    public ObservableCollection<IRecentFileEntry> RecentItems => sharedRecords.RecentItems;

    /// <summary>
    ///     Adds or updates the specified file path in the recent files list.
    /// </summary>
    public void AddOrUpdateRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Log.Warning("Attempted to add empty file path to recent files");
            return;
        }

        sharedRecords.UpsertRecentFile(filePath, DateTime.Now);
    }

    /// <summary>
    ///     Removes the specified file path from the recent files list.
    /// </summary>
    public void RemoveRecentFile(IRecentFileEntry recentFileEntry)
    {
        sharedRecords.DeleteRecentFile(recentFileEntry);
        Log.Information("Removed a recent file entry");
    }
}