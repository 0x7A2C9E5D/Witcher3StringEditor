namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for recent item information
///     Represents metadata about a recently opened file including its path and opening time
/// </summary>
public interface IRecentItem
{
    /// <summary>
    ///     Gets the file path of the recently opened item
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     Gets or sets the time when the item was last opened
    /// </summary>
    public DateTime OpenedTime { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the item is marked
    /// </summary>
    public bool IsMarked { get; set; }
}