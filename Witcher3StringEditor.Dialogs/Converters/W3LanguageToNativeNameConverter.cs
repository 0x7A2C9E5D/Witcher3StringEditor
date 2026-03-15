using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     A value converter that converts W3Language enum values to their corresponding native language names
///     Uses reflection to retrieve the DescriptionAttribute from the enum value and creates a CultureInfo
///     to get the native name of the language
/// </summary>
public class W3LanguageToNativeNameConverter : IValueConverter
{
    /// <summary>
    ///     Converts a W3Language enum value to its corresponding native language name
    /// </summary>
    /// <param name="value">The W3Language enum value to convert</param>
    /// <param name="targetType">The type of the binding target property (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>The native name of the language, or DependencyProperty.UnsetValue if conversion fails</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Check if the value is a valid W3Language enum, if not return UnsetValue
        if (value is not W3Language language) return DependencyProperty.UnsetValue;

        // Get the field info for the language enum value
        // Retrieve the DescriptionAttribute from the field
        // Create a CultureInfo from the description and return its native name
        return new CultureInfo(typeof(W3Language).GetField(language.ToString())!
            .GetCustomAttribute<DescriptionAttribute>()!.Description).NativeName;
    }

    /// <summary>
    ///     Converts a native language name back to a W3Language enum value (not implemented)
    /// </summary>
    /// <param name="value">The value to convert back</param>
    /// <param name="targetType">The type to convert to (not used in this implementation)</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic (not used in this implementation)</param>
    /// <param name="culture">The culture to use in the converter (not used in this implementation)</param>
    /// <returns>Throws NotImplementedException as this operation is not supported</returns>
    /// <exception cref="NotImplementedException">This method is not implemented</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}