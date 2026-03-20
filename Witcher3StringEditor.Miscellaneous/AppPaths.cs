namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     A class that provides helper methods for working with paths.
/// </summary>
public static class AppPaths
{
    /// <summary>
    ///     The application data directory.
    /// </summary>
    public static readonly string AppDataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            DebugHelper.IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor");

    /// <summary>
    ///     The log directory.
    /// </summary>
    public static readonly string LogDirectory = Path.Combine(AppDataDirectory, "Logs");

    /// <summary>
    ///     The backup directory.
    /// </summary>
    public static readonly string BackupDirectory = Path.Combine(AppDataDirectory, "Backup");

    /// <summary>
    ///     The dictionary directory.
    /// </summary>
    public static readonly string DictionaryDirectory = Path.Combine(AppDataDirectory, "Dictionaries");
}