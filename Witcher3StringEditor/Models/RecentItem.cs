using CommunityToolkit.Mvvm.ComponentModel;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents a recent item model
///     Implements the IRecentItem interface and provides observable properties for data binding
///     This class stores metadata about a recently opened file including its path and opening time
/// </summary>
internal partial class RecentItem : ObservableObject, IRecentItem
{
    /// <summary>
    ///     Gets or sets the file path of the recently opened item
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string filePath;

    /// <summary>
    ///     Gets or sets a value indicating whether the item is marked
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private bool isMarked;

    /// <summary>
    ///     Gets or sets the time when the item was last opened
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private DateTime openedTime;

    /// <summary>
    ///     Initializes a new instance of the RecentItem class with specified values
    /// </summary>
    /// <param name="filePath">The file path of the recently opened item</param>
    /// <param name="openedTime">The time when the item was opened</param>
    /// <param name="isMarked">A value indicating whether the item is marked (default is false)</param>
    public RecentItem(string filePath, DateTime openedTime, bool isMarked = false)
    {
        IsMarked = isMarked;
        FilePath = filePath;
        OpenedTime = openedTime;
    }
}