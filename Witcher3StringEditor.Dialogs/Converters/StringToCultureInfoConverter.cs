using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     A value converter that converts between string representations and CultureInfo objects
///     Used in XAML data binding to convert language codes to CultureInfo objects and vice versa
/// </summary>
internal class StringToCultureInfoConverter : IValueConverter
{
    /// <summary>
    ///     Converts a string to a CultureInfo object
    /// </summary>
    /// <param name="value">The string value to convert (expected to be a culture name like "en-US")</param>
    /// <param name="targetType">The type of the binding target property (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>A CultureInfo object corresponding to the string, or DependencyProperty.UnsetValue if conversion fails</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is not string ci) return DependencyProperty.UnsetValue;
            return !string.IsNullOrWhiteSpace(ci) ? new CultureInfo(ci) : DependencyProperty.UnsetValue;
        }
        catch (CultureNotFoundException)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    ///     Converts a CultureInfo object back to its string representation
    /// </summary>
    /// <param name="value">The CultureInfo object to convert back to a string</param>
    /// <param name="targetType">The type to convert to (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>The name of the CultureInfo as a string, or DependencyProperty.UnsetValue if conversion fails</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CultureInfo ci ? ci.Name : DependencyProperty.UnsetValue;
    }
}