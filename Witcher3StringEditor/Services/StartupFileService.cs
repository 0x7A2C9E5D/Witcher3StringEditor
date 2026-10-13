using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Handles the file a launch was asked to open (for example through "Open with")
/// </summary>
internal sealed class StartupFileService : IStartupFileService
{
    // The file extensions a command-line path may carry (for example through "Open with")
    private static readonly string[] SupportedExtensions = [".csv", ".w3strings", ".xlsx"];

    /// <summary>
    ///     Gets the startup file from the command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>
    ///     Returns the first supported file path found in the arguments, or null if none is found.
    /// </returns>
    public string? GetStartupFile(string[] args)
    {
        return args.FirstOrDefault(arg =>
            SupportedExtensions.Contains(Path.GetExtension(arg), StringComparer.OrdinalIgnoreCase) &&
            File.Exists(arg));
    }

    /// <summary>
    ///     Opens the specified file.
    /// </summary>
    /// <param name="filePath">The file path to open.</param>
    public void OpenFile(string filePath)
    {
        WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<string, bool>(filePath),
            MessageTokens.RecentFileOpened);
    }
}