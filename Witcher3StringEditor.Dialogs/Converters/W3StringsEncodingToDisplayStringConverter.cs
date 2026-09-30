using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

/// <summary>
///     W3Strings payload encoding to game version display string converter
///     Names the game generation whose containers store the encoding, which is what the user picks
/// </summary>
public class W3StringsEncodingToDisplayStringConverter : IValueConverter
{
    /// <summary>
    ///     Converts a payload encoding to its display string
    /// </summary>
    /// <param name="value">The Encoding value to display</param>
    /// <param name="targetType">Target type (should be string type)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="culture">Culture information</param>
    /// <returns>The name of the game version that stores the payload encoding</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Encoding encoding) return string.Empty;
        // Only the two encodings a container can store are offered, each named after the game version that uses it
        return encoding.CodePage == Encoding.UTF8.CodePage ? "Remastered" : "Classic / Next-Gen";
    }

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
