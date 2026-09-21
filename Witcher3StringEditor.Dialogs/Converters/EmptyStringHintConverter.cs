using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     A converter for handling empty strings and custom hints
/// </summary>
public class EmptyStringHintConverter : IMultiValueConverter
{
    /// <summary>
    ///     The placeholder shown when no value and no custom hint are available
    /// </summary>
    private static string EmptyPlaceholder => "-";

    /// <summary>
    ///     Converts an empty string to a placeholder string or a custom hint string if provided.
    /// </summary>
    /// <param name="values">
    ///     The values to convert; the first is the input string, the optional second is the custom hint
    /// </param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">An optional fallback hint</param>
    /// <param name="culture">The culture of the conversion</param>
    /// <returns>The input string, the custom hint or the placeholder</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] == DependencyProperty.UnsetValue)
            return parameter as string ?? EmptyPlaceholder;
        var inputString = values[0].ToString()?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(inputString)) return inputString;
        if (values.Length < 2 || values[1] == DependencyProperty.UnsetValue)
            return parameter as string ?? EmptyPlaceholder;
        var customHint = values[1].ToString();
        return !string.IsNullOrWhiteSpace(customHint) ? customHint : EmptyPlaceholder;
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