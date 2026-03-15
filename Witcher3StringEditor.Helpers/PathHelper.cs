namespace Witcher3StringEditor.Helpers;

public static class PathHelper
{
    public static readonly string AppDataDirectory = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        DebugHelper.IsDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor");
    
    public static readonly string LogsDirectory = Path.Combine(AppDataDirectory, "Logs");
    
    public static readonly string BackupDirectory = Path.Combine(AppDataDirectory, "Backup");
    
    public static readonly string DictionariesDirectory = Path.Combine(AppDataDirectory, "Directories");
}