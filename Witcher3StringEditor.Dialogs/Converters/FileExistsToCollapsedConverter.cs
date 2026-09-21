using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     File existence to visibility converter
///     Returns Visibility.Collapsed when file exists (hide the element)
///     Returns Visibility.Visible when file does not exist (show the element)
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
    ///     Visibility.Collapsed when path is null/empty or file exists (hide element)
    ///     Visibility.Visible when file does not exist (show element)
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || File.Exists(path)) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    /// <summary>
    ///     Reverse conversion (not implemented)
    /// </summary>
    /// <param name="value">Visibility value</param>
    /// <param name="targetType">Target type</param>
    /// <param name="parameter">Converter parameter</param>
    /// <param name="culture">Culture information</param>
    /// <returns>Throws NotImplementedException as this operation is not supported</returns>
    /// <exception cref="NotImplementedException">This method is not implemented</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}