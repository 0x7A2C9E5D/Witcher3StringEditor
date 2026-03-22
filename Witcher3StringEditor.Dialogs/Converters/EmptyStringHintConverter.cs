using System.Globalization;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///   A converter for handling empty strings and custom hints.
/// </summary>
public class EmptyStringHintConverter : IMultiValueConverter
{
    private static string EmptyPlaceholder => "-";
    
    /// <summary>
    ///   Converts an empty string to a placeholder string or a custom hint string if provided.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var inputString = values[0].ToString()?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(inputString)) return inputString;
        if (values.Length < 2) return parameter as string ?? EmptyPlaceholder;
        var customHint = values[1].ToString();
        return !string.IsNullOrWhiteSpace(customHint) ? customHint : EmptyPlaceholder;
    }

    /// <summary>
    ///   Converts a custom hint string back to an empty string.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetTypes"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}