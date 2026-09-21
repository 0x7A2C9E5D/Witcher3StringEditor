using System.Collections.ObjectModel;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for recent files service operations
/// </summary>
public interface IRecentFilesService
{
    /// <summary>
    ///     Gets the collection of recently opened items
    /// </summary>
    ObservableCollection<IRecentFileEntry> RecentItems { get; }

    /// <summary>
    ///     Adds or updates a recent file entry
    /// </summary>
    /// <param name="filePath"></param>
    void AddOrUpdateRecentFile(string filePath);

    /// <summary>
    ///     Removes a recent file entry
    /// </summary>
    /// <param name="recentFileEntry"></param>
    void RemoveRecentFile(IRecentFileEntry recentFileEntry);
}