using System.Collections.ObjectModel;

namespace Witcher3StringEditor.Contracts.Abstractions;

public interface IRecentFilesService
{
    ObservableCollection<IRecentItem> RecentItems { get; }
    void AddOrUpdateRecentFile(string filePath);

    bool RemoveRecentFile(IRecentItem recentItem);
}