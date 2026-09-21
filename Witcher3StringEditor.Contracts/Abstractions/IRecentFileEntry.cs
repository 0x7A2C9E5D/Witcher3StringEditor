namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for recent item information
///     Represents metadata about a recently opened file including its path and opening time
/// </summary>
public interface IRecentFileEntry
{
    /// <summary>
    ///     Gets the file path of the recently opened item
    ///     Implemented as a full, normalized path so that duplicate detection is stable
    /// </summary>
    string FilePath { get; }

    /// <summary>
    ///     Gets or sets the time when the item was last opened
    ///     Expressed as a <see cref="DateTime" /> so the ordering of the recent list stays correct
    ///     independently of the machine's local time zone
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    DateTime OpenedTime { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the item is marked
    ///     "Marked" is transient, user-visible UI state (used to pin/highlight an entry); it is persisted
    ///     together with the other recent item data and must not be interpreted by the service layer
    /// </summary>
    bool IsMarked { get; set; }
}