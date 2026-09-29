using System.Globalization;
using System.Windows.Data;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     W3Strings container version to display string converter
///     Names the payload encoding the version implies, which is what the user picks
///     and is a technical fact that needs no translation
/// </summary>
public class W3StringsVersionToDisplayStringConverter : IValueConverter
{
    /// <summary>
    ///     Converts a container version to its display string
    /// </summary>
    /// <param name="value">The W3StringsVersion value to display</param>
    /// <param name="targetType">Target type (should be string type)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="culture">Culture information</param>
    /// <returns>The encoding of the payload</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        W3StringsVersion.Classic => "UTF-16LE",
        W3StringsVersion.Legacy => "UTF-16LE",
        W3StringsVersion.Utf8 => "UTF-8",
        _ => string.Empty
    };

    /// <summary>
    ///     Reverse conversion (not implemented)
    /// </summary>
    /// <param name="value">Display string</param>
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
