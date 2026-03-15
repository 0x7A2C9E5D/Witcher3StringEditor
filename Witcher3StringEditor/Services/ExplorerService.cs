using System.Diagnostics;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides functionality to open paths in Windows Explorer
///     Implements the IExplorerService interface to handle opening file system paths
/// </summary>
internal class ExplorerService : IExplorerService
{
    /// <summary>
    ///     Opens the specified path in Windows Explorer
    /// </summary>
    /// <param name="path">The path to open in Windows Explorer</param>
    public void Open(string path)
    {
        using var _ = Process.Start("explorer.exe", path);
    }
}