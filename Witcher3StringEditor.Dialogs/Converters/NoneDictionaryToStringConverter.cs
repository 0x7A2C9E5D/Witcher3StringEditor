using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Witcher3StringEditor.Locales;

namespace Witcher3StringEditor.Dialogs.Converters;

public class NoneDictionaryToStringConverter : IValueConverter
{
    /// <summary>
    ///     Converts a file path to a file name
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name) return DependencyProperty.UnsetValue;
        return string.IsNullOrWhiteSpace(name) ? Strings.NoDictionary : name;
    }

    /// <summary>
    ///     Not implemented
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