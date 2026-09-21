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
/// <remarks>
///     The localized text is resolved from the ambient <see cref="Strings" /> resources, so it follows the
///     application language rather than the converter culture. The second bound value is not read by the
///     conversion itself; it only re-evaluates the binding when the language setting changes
/// </remarks>
internal class W3FileTypeToLocalizedStringConverter : IMultiValueConverter
{
    /// <summary>
    ///     Converts a W3FileType enum value to its corresponding localized string representation
    /// </summary>
    /// <param name="values">An array of values to convert, where the first element is expected to be a W3FileType enum value</param>
    /// <param name="targetType">The type of the binding target property (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>
    ///     The localized string representation of the file type; unmapped values fall back to the enum name so a
    ///     missing mapping is visible instead of rendering an empty cell
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not W3FileType fileType) 
            return DependencyProperty.UnsetValue;
        return fileType switch
        {
            W3FileType.Csv => Strings.FileFormatTextFile,
            W3FileType.W3Strings => Strings.FileFormatWitcher3StringsFile,
            W3FileType.Excel => Strings.FileFormatExcelWorkbook,
            _ => fileType.ToString()
        };
    }

    /// <summary>
    ///     Reports that no value should be written back to the sources
    /// </summary>
    /// <param name="value">The value to convert back</param>
    /// <param name="targetTypes">The types of the binding source properties</param>
    /// <param name="parameter">The converter parameter</param>
    /// <param name="culture">The culture of the conversion</param>
    /// <returns>
    ///     <see cref="Binding.DoNothing" /> for every source, because this converter is display-only. Throwing here
    ///     would surface as a runtime binding failure whenever WPF evaluates the reverse direction
    /// </returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [.. Enumerable.Repeat(Binding.DoNothing, targetTypes.Length)];
    }
}