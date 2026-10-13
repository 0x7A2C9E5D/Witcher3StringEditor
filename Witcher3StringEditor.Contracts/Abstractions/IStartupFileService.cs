namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for handling the file a launch was asked to open
///     Resolves the file passed on the command line (for example through "Open with")
///     and requests the application to open it
/// </summary>
public interface IStartupFileService
{
    /// <summary>
    ///     Gets the supported file path passed on the command line
    /// </summary>
    /// <param name="args">The command-line arguments</param>
    /// <returns>The first existing argument with a supported extension, or null when there is none</returns>
    public string? GetStartupFile(string[] args);

    /// <summary>
    ///     Requests the main window to open the specified file
    /// </summary>
    /// <param name="filePath">The path of the file to open</param>
    public void OpenFile(string filePath);
}
