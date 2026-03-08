using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Witcher3StringEditor.Dialogs.Converters;

public class FilePathToFileNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath)
            return DependencyProperty.UnsetValue;
        return filePath == string.Empty ? "不使用词典" : Path.GetFileNameWithoutExtension(filePath);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}