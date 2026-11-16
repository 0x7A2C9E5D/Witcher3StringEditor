using System.IO;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Witcher3StringEditor.Locales;

namespace Witcher3StringEditor.ViewModels;

/// <summary>
///     Which files can be read and how the open file dialog offers them; shared by opening and merging.
/// </summary>
internal partial class MainWindowViewModel
{
    /// <summary>
    ///     Describes the open file dialog that lists the formats this editor reads.
    /// </summary>
    private static OpenFileDialogSettings CreateOpenFileDialogSettings()
    {
        return new OpenFileDialogSettings
        {
            Filters =
            [
                new FileFilter(Strings.FileFormatSupported, [".csv", ".xlsx", ".w3strings"]),
                new FileFilter(Strings.FileFormatTextFile, ".csv"),
                new FileFilter(Strings.FileFormatExcelWorkbook, ".xlsx"),
                new FileFilter(Strings.FileFormatWitcher3StringsFile, ".w3strings")
            ]
        };
    }

    /// <summary>
    ///     Gets a value indicating whether a file has one of the extensions this editor reads.
    /// </summary>
    private static bool IsSupportedFile(string path)
    {
        return Path.GetExtension(path) is ".csv" or ".w3strings" or ".xlsx";
    }
}