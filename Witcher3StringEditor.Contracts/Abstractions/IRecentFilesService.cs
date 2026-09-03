using System.Collections.ObjectModel;

namespace Witcher3StringEditor.Contracts.Abstractions;

public interface IRecentFilesService
{
    ObservableCollection<IRecentFileEntry> RecentItems { get; }

    void AddOrUpdateRecentFile(string filePath);

    void RemoveRecentFile(IRecentFileEntry recentFileEntry);
}