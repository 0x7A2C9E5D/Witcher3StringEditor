using System.Collections.ObjectModel;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

internal class RecentFilesService(IAppSettings appSettings, int maxRecentItems = 20) : IRecentFilesService
{
    public ObservableCollection<IRecentItem> RecentItems => appSettings.RecentItems;

    public void AddOrUpdateRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Log.Warning("Attempted to add empty file path to recent files");
            return;
        }

        var recentItems = appSettings.RecentItems;
        var existingItem = recentItems.FirstOrDefault(x =>
            string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            existingItem.OpenedTime = DateTime.Now;
            recentItems.Remove(existingItem);
            recentItems.Insert(0, existingItem);
            Log.Debug("Updated recent file: {FilePath}", filePath);
        }
        else
        {
            var newItem = new RecentItem(filePath, DateTime.Now);
            recentItems.Insert(0, newItem);
            Log.Information("Added new recent file: {FilePath}", filePath);
        }

        CleanupExcessItems(recentItems);
    }

    public bool RemoveRecentFile(IRecentItem recentItem)
    {
        var removed = appSettings.RecentItems.Remove(recentItem);
        if (removed)
            Log.Information("Removed recent file: {FilePath}", recentItem.FilePath);
        else
            Log.Error("Failed to remove recent file: {FilePath}", recentItem.FilePath);
        return removed;
    }

    private void CleanupExcessItems(ObservableCollection<IRecentItem> recentItems)
    {
        if (recentItems.Count <= maxRecentItems)
            return;

        var unmarkedItems = recentItems.Where(x => !x.IsMarked).ToList();
        var itemsToRemoveCount = recentItems.Count - maxRecentItems;

        if (itemsToRemoveCount <= 0 || itemsToRemoveCount > unmarkedItems.Count)
            return;

        var itemsToRemove = unmarkedItems
            .OrderByDescending(x => x.OpenedTime)
            .Take(itemsToRemoveCount)
            .ToList();

        foreach (var item in itemsToRemove)
        {
            recentItems.Remove(item);
            Log.Debug("Removed old recent file: {FilePath}", item.FilePath);
        }
    }
}