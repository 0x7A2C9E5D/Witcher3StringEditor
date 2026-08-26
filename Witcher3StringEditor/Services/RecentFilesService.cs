using System.Collections.ObjectModel;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

internal class RecentFilesService(ObservableCollection<IRecentFileEntry> recentItems) : IRecentFilesService
{
    public ObservableCollection<IRecentFileEntry> RecentItems { get; } = recentItems;

    public void AddOrUpdateRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Log.Warning("Attempted to add empty file path to recent files");
            return;
        }

        var existingItem = RecentItems.FirstOrDefault(x =>
            string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            existingItem.OpenedTime = DateTime.Now;
            Log.Debug("Updated recent file: {FilePath}", filePath);
        }
        else
        {
            RecentItems.Add(new RecentFileEntry(filePath, DateTime.Now));
            Log.Information("Added new recent file: {FilePath}", filePath);
        }
    }

    public bool RemoveRecentFile(IRecentFileEntry recentFileEntry)
    {
        var removed = RecentItems.Remove(recentFileEntry);
        if (removed)
            Log.Information("Removed recent file: {FilePath}", recentFileEntry.FilePath);
        else
            Log.Error("Failed to remove recent file: {FilePath}", recentFileEntry.FilePath);
        return removed;
    }
}