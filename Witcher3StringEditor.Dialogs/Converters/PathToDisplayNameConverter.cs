using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

public class PathToDisplayNameConverter : IValueConverter
{
    /// <summary>
    ///   Converts a file path to a file name
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath)
            return DependencyProperty.UnsetValue;
        return filePath == string.Empty
            ? I18NExtension.Translate("NoDictionary")!
            : Path.GetFileNameWithoutExtension(filePath);
    }

    /// <summary>
    ///  Not implemented
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}