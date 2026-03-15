using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Locales;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     A multi-value converter that converts W3FileType enum values to their corresponding localized string
///     representations
///     Used in XAML data binding to display user-friendly file type descriptions
/// </summary>
internal class W3FileTypeToLocalizedStringConverter : IMultiValueConverter
{
    /// <summary>
    ///     Converts a W3FileType enum value to its corresponding localized string representation
    /// </summary>
    /// <param name="values">An array of values to convert, where the first element is expected to be a W3FileType enum value</param>
    /// <param name="targetType">The type of the binding target property (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>The localized string representation of the file type, or DependencyProperty.UnsetValue if conversion fails</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not W3FileType fileType) return DependencyProperty.UnsetValue;
        return fileType switch
        {
            W3FileType.Csv => Strings.FileFormatTextFile,
            W3FileType.W3Strings => Strings.FileFormatWitcher3StringsFile,
            W3FileType.Excel => Strings.FileFormatExcelWorkbook,
            _ => DependencyProperty.UnsetValue
        };
    }

    /// <summary>
    ///     Converts a localized string back to a W3FileType enum value (not implemented)
    /// </summary>
    /// <param name="value">The value to convert back</param>
    /// <param name="targetTypes">The types of the binding target properties</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>Throws NotImplementedException as this operation is not supported</returns>
    /// <exception cref="NotImplementedException">This method is not implemented</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}