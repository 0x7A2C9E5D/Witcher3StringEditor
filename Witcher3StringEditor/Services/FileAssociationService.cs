using Microsoft.Win32;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Manages the Windows file association for <c>.w3strings</c> files
///     Writes to the per-user <c>HKCU\Software\Classes</c> branch, so registering the
///     association never requires administrator privileges
/// </summary>
internal sealed class FileAssociationService : IFileAssociationService
{
    private const string ProgramId = "Witcher3StringEditor.w3strings"; // ProgID identifying this application
    private const string Extension = ".w3strings"; // The file extension handled by this application
    private const string ClassesPath = @"Software\Classes"; // Per-user registry branch holding class registrations
    private const string Description = "The Witcher 3 Strings File"; // Friendly type description shown by the shell

    /// <summary>
    ///     Gets a value indicating whether the file association is currently registered
    ///     The association only counts as registered while it points at the executable that is running now
    /// </summary>
    public bool IsRegistered
    {
        get
        {
            using var extensionKey = Registry.CurrentUser.OpenSubKey($@"{ClassesPath}\{Extension}");
            if (extensionKey?.GetValue(string.Empty) as string != ProgramId) return false; // The extension is not ours
            using var commandKey = Registry.CurrentUser.OpenSubKey($@"{ClassesPath}\{ProgramId}\shell\open\command");
            var registeredPath = GetExecutablePathFromCommand(commandKey?.GetValue(string.Empty) as string);
            var currentPath = Environment.ProcessPath;
            // A stale association (for example after the application is moved) does not count as registered
            return currentPath is not null && registeredPath is not null &&
                   string.Equals(registeredPath, currentPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Registers the file association so that <c>.w3strings</c> files can be opened with this application
    ///     The extension is pointed at this application's ProgID and the ProgID is added to the
    ///     "Open with" list, so the application shows up both as the default handler and in the shell menu
    /// </summary>
    public void Register()
    {
        var exePath = GetExecutablePath(); // The command the shell launches when a file is opened
        using var classesKey = Registry.CurrentUser.CreateSubKey(ClassesPath); // Per-user class registrations

        using (var extensionKey = classesKey.CreateSubKey(Extension))
        {
            extensionKey.SetValue(string.Empty, ProgramId); // Point the extension at this application
            using var openWithKey = extensionKey.CreateSubKey("OpenWithProgids"); // List entry for "Open with"
            openWithKey.SetValue(ProgramId, Array.Empty<byte>(), RegistryValueKind.Binary);
        }

        using (var programKey = classesKey.CreateSubKey(ProgramId))
        {
            programKey.SetValue(string.Empty, Description); // Friendly type description
            using var iconKey = programKey.CreateSubKey("DefaultIcon"); // Icon used by the shell
            iconKey.SetValue(string.Empty, $"\"{exePath}\",0");
            using var commandKey = programKey.CreateSubKey(@"shell\open\command"); // Command launched on open
            commandKey.SetValue(string.Empty, $"\"{exePath}\" \"%1\"");
        }

        Log.Information("Registered .w3strings file association pointing to {ExePath}", exePath);
    }

    /// <summary>
    ///     Removes the file association previously registered by this application
    /// </summary>
    public void Unregister()
    {
        using var classesKey = Registry.CurrentUser.OpenSubKey(ClassesPath, true); // Open the class branch for writing
        if (classesKey is null) return;
        classesKey.DeleteSubKeyTree(ProgramId, false); // Remove the ProgID key registered by this application

        using var extensionKey = classesKey.OpenSubKey(Extension, true);
        if (extensionKey is null) return;
        if (extensionKey.GetValue(string.Empty) as string == ProgramId) // Clear the default only when it is ours
            extensionKey.DeleteValue(string.Empty, false);
        using var openWithKey = extensionKey.OpenSubKey("OpenWithProgids", true);
        openWithKey?.DeleteValue(ProgramId, false); // Remove this application from the "Open with" list

        Log.Information("Unregistered .w3strings file association");
    }

    /// <summary>
    ///     Gets the path to the currently running executable
    /// </summary>
    /// <returns>The full path of the executable the shell should launch</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current process path is unavailable</exception>
    private static string GetExecutablePath()
    {
        return Environment.ProcessPath ??
               throw new InvalidOperationException("The current process path could not be determined.");
    }

    /// <summary>
    ///     Gets the executable path stored in a shell open command value
    /// </summary>
    /// <param name="command">The command value, for example <c>"C:\app.exe" "%1"</c></param>
    /// <returns>The executable path, or null when the command is empty or malformed</returns>
    private static string? GetExecutablePathFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command) || !command.StartsWith('"')) return null;
        var endQuote = command.IndexOf('"', 1);
        return endQuote > 1 ? command[1..endQuote] : null;
    }
}