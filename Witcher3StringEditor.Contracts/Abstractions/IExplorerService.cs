namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for opening paths in Windows Explorer
///     Provides a method to open file system paths using the system's default file explorer
/// </summary>
public interface IExplorerService
{
    /// <summary>
    ///     Opens the specified path in Windows Explorer
    /// </summary>
    /// <param name="path">The path to open in Windows Explorer</param>
    public void Open(string path);
}