using System.Collections.ObjectModel;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for recent files service operations
/// </summary>
public interface IRecentFilesService
{
    /// <summary>
    ///     Gets the collection of recently opened items
    ///     The collection is owned by the service; entries are ordered with the most recently opened item first
    /// </summary>
    ObservableCollection<IRecentFileEntry> RecentItems { get; }

    /// <summary>
    ///     Adds a new recent file entry or refreshes the open time of an existing one
    /// </summary>
    /// <param name="filePath">The path of the opened file</param>
    /// <remarks>
    ///     A null, empty or white-space <paramref name="filePath" /> is treated as a no-op and logged as a warning.
    ///     Paths are normalized before comparison, so different textual forms of the same file are not duplicated.
    ///     Matching is case-insensitive and refresh does not change the position of the existing entry.
    /// </remarks>
    void AddOrUpdateRecentFile(string filePath);

    /// <summary>
    ///     Removes a recent file entry
    /// </summary>
    /// <param name="recentFileEntry">The entry to remove; must be an instance contained in <see cref="RecentItems" /></param>
    /// <remarks>
    ///     A null argument is ignored and logged as a warning. Removal uses instance identity, so a
    ///     structurally equal but different instance is not removed and the failure is logged.
    /// </remarks>
    void RemoveRecentFile(IRecentFileEntry recentFileEntry);
}