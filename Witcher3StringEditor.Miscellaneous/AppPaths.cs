namespace Witcher3StringEditor.Miscellaneous;

/// <summary>
///     A class that provides helper methods for working with paths.
/// </summary>
public static class AppPaths
{
    /// <summary>
    ///     The base application folder name, shared with the single instance mutex name so the debug and release
    ///     scopes cannot drift apart
    /// </summary>
    private const string AppFolderName = "Witcher3StringEditor";

    /// <summary>
    ///     The folder name used by debug builds
    /// </summary>
    private const string DebugAppFolderName = AppFolderName + "_Debug";

    /// <summary>
    ///     The application data directory.
    ///     The folder is composed under the roaming application data folder; when that folder cannot be resolved the
    ///     paths would silently become relative to the current working directory, so the failure is surfaced instead
    /// </summary>
    /// <exception cref="InvalidOperationException">The application data folder could not be resolved</exception>
    public static readonly string AppDataDirectory =
        Path.Combine(GetApplicationDataFolder(),
            DebugHelper.IsDebug ? DebugAppFolderName : AppFolderName);

    /// <summary>
    ///     The application settings path.
    /// </summary>
    public static readonly string ConfigPath = Path.Combine(AppDataDirectory, "AppSettings.json");

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

    /// <summary>
    ///     Creates the directories the application writes to
    /// </summary>
    /// <remarks>
    ///     Consumers can call this instead of re-implementing <see cref="Directory.CreateDirectory(string)" />,
    ///     so enumerating the log or dictionary directory cannot fail with a missing directory
    /// </remarks>
    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(DictionaryDirectory);
    }

    /// <summary>
    ///     Gets the roaming application data folder
    /// </summary>
    /// <returns>The resolved folder</returns>
    /// <exception cref="InvalidOperationException">The folder could not be resolved</exception>
    private static string GetApplicationDataFolder()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return applicationData is { Length: > 0 }
            ? applicationData
            : throw new InvalidOperationException(
                "The application data folder could not be resolved, so the application paths cannot be composed.");
    }
}