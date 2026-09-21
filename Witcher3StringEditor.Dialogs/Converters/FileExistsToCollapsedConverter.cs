using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     File existence to visibility converter
///     Returns Visibility.Collapsed when the path is empty or the file exists (hide the element)
///     Returns Visibility.Visible when a non-empty path does not exist (show the element)
///     Used primarily to control display of invalid path indicators in UI
/// </summary>
public class FileExistsToCollapsedConverter : IValueConverter
{
    /// <summary>
    ///     Converts file path to visibility state
    /// </summary>
    /// <param name="value">File path string to check</param>
    /// <param name="targetType">Target type (should be Visibility type)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="culture">Culture information</param>
    /// <returns>
    ///     Visibility.Collapsed when the path is null/empty/white-space or the file exists (hide element)
    ///     Visibility.Visible when a non-empty path does not exist (show element)
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return Visibility.Collapsed;
        if (!IsPlausiblePath(path)) return Visibility.Visible;
        return File.Exists(path) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    ///     Reverse conversion (not supported)
    /// </summary>
    /// <param name="value">Visibility value</param>
    /// <param name="targetType">Target type</param>
    /// <param name="parameter">Converter parameter</param>
    /// <param name="culture">Culture information</param>
    /// <returns>
    ///     <see cref="Binding.DoNothing" />, because this converter is display-only. Throwing would surface as a
    ///     runtime binding failure whenever WPF evaluates the reverse direction
    /// </returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    /// <summary>
    ///     Determines whether a path can plausibly point at an existing file, without touching the file system
    /// </summary>
    /// <param name="path">The path to inspect</param>
    /// <returns>True when the path is rooted and contains no invalid characters</returns>
    private static bool IsPlausiblePath(string path)
    {
        return Path.IsPathFullyQualified(path) && path.IndexOfAny(Path.GetInvalidPathChars()) < 0;
    }
}