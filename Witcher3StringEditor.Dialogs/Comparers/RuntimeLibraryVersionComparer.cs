using System.ComponentModel;
using Microsoft.Extensions.DependencyModel;
using Syncfusion.Data;

namespace Witcher3StringEditor.Dialogs.Comparers;

/// <summary>
///     A custom comparer for sorting RuntimeLibrary objects based on their version strings
/// </summary>
public class RuntimeLibraryVersionComparer : IComparer<object>, ISortDirection
{
    /// <summary>
    ///     Compares two RuntimeLibrary objects based on their version strings
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>
    ///     A value indicating whether the first object is less than, equal to, or greater than the second object
    /// </returns>
    public int Compare(object? x, object? y)
    {
        // Null entries, which the grid can hand over while its items are replaced, sort first and follow
        // the sort direction like any other comparison
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return Apply(-1);
        if (y is null) return Apply(1);

        // Extract version strings from RuntimeLibrary objects
        var versionStringX = ((RuntimeLibrary)x).Version;
        var versionStringY = ((RuntimeLibrary)y).Version;

        // Version parsing rejects NuGet style versions such as "2.0.0-preview.3", so those pairs fall back
        // to a string comparison; the direction is applied once, so the fallback follows it too
        var comparisonResult = Version.TryParse(versionStringX, out var versionX) &&
                               Version.TryParse(versionStringY, out var versionY)
            ? versionX.CompareTo(versionY)
            : string.Compare(versionStringX, versionStringY, StringComparison.Ordinal);

        return Apply(comparisonResult);
    }

    /// <summary>
    ///     Gets or sets the sort direction for the comparison
    /// </summary>
    public ListSortDirection SortDirection { get; set; }

    /// <summary>
    ///     Applies the sort direction to a comparison result
    /// </summary>
    /// <param name="result">The ascending comparison result</param>
    /// <returns>The comparison result in the configured direction</returns>
    private int Apply(int result)
    {
        return SortDirection == ListSortDirection.Descending ? -result : result;
    }
}