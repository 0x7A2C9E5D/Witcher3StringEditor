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
    ///     The file is replaced atomically so concurrent instances never read a partially written file.
    /// </summary>
    /// <typeparam name="T">The type of settings to save</typeparam>
    /// <param name="settings">The settings to save</param>
    public void Save<T>(T settings)
    {
        var json = JsonSerializer.Serialize(settings, jsonSerializerOptions);
        var temporary = $"{filePath}.{Environment.ProcessId}.tmp";
        for (var attempt = 1; attempt <= 3; attempt++)
            try
            {
                File.WriteAllText(temporary, json);
                File.Move(temporary, filePath, true);
                return;
            }
            catch (IOException) when (attempt < 3)
            {
                Thread.Sleep(50 * attempt);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Failed to save the application settings");
                return;
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