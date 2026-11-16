using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides configuration file saving and loading functionality
///     Implements the ISettingsPersistenceService interface to handle serialization and deserialization of settings
/// </summary>
internal class SettingsPersistenceService : ISettingsPersistenceService
{
    private const int SaveAttempts = 3;

    /// <summary>
    ///     Serializes every instance that saves the settings file, so a leader handover can never
    ///     interleave two writes on the same file.
    /// </summary>
    private static readonly string mutexName = DebugHelper.IsDebug
        ? "Witcher3StringEditor_SettingsFile_Debug"
        : "Witcher3StringEditor_SettingsFile";

    private readonly string filePath;

    /// <summary>
    ///     JSON serializer options
    /// </summary>
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new EncodingJsonConverter(),
            new LanguageJsonConverter(),
            new InterfaceJsonConverter<IRecentFileEntry, RecentItem>(),
            new InterfaceJsonConverter<IBackupItem, BackupItem>(),
            new ObservableCollectionJsonConverter<IBackupItem>(),
            new ObservableCollectionJsonConverter<IRecentFileEntry>()
        }
    };

    /// <summary>
    ///     Provides configuration file saving and loading functionality
    ///     Implements the ISettingsPersistenceService interface to handle serialization and deserialization of settings
    /// </summary>
    /// <param name="filePath">The path to the configuration file</param>
    public SettingsPersistenceService(string filePath)
    {
        this.filePath = filePath;
        var configDirectory = Directory.GetParent(filePath)!.FullName;
        Directory.CreateDirectory(configDirectory);
    }

    /// <summary>
    ///     Provides configuration file saving and loading functionality
    ///     Implements the ISettingsPersistenceService interface to handle serialization and deserialization of settings
    /// </summary>
    public SettingsPersistenceService() : this(AppPaths.ConfigPath)
    {
    }

    /// <summary>
    ///     Saves the specified settings to a configuration file.
    ///     The file is replaced atomically so concurrent instances never read a partially written file,
    ///     and the write itself is serialized across processes.
    /// </summary>
    /// <typeparam name="T">The type of settings to save</typeparam>
    /// <param name="settings">The settings to save</param>
    public void Save<T>(T settings)
    {
        var json = JsonSerializer.Serialize(settings, jsonSerializerOptions);
        using var mutex = new Mutex(false, mutexName);
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(3));
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                Log.Warning("Timed out waiting for the application settings file lock");
                return;
            }

            WriteAtomically(json);
        }
        finally
        {
            if (acquired) mutex.ReleaseMutex();
        }
    }

    private void WriteAtomically(string json)
    {
        var temporary = $"{filePath}.{Environment.ProcessId}.tmp";
        for (var attempt = 1; attempt <= SaveAttempts; attempt++)
            try
            {
                File.WriteAllText(temporary, json);
                File.Move(temporary, filePath, true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == SaveAttempts)
                {
                    Log.Warning(ex, "Failed to save the application settings");
                    return;
                }

                Thread.Sleep(50 * attempt);
            }
    }

    /// <summary>
    ///     Loads settings from a configuration file
    /// </summary>
    /// <typeparam name="T">The type of settings to load</typeparam>
    /// <returns>The loaded settings, or a new instance if the file does not exist</returns>
    public T Load<T>() where T : new()
    {
        if (!File.Exists(filePath)) // Check if config file exists
            return new T(); // Create new instance if file doesn't exist

        var content = File.ReadAllText(filePath); // Read file content
        var result = JsonSerializer.Deserialize<T>(content, jsonSerializerOptions); // Deserialize content
        return result ?? new T(); // Return deserialized object or new instance if null
    }
}