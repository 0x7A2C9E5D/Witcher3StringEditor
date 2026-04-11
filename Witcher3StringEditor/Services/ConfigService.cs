using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides configuration file saving and loading functionality
///     Implements the IConfigService interface to handle serialization and deserialization of settings
/// </summary>
internal class ConfigService(string filePath) : IConfigService
{
    /// <summary>
    ///      JSON serializer options
    /// </summary>
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new InterfaceJsonConverter<IRecentItem,RecentItem>(),
            new InterfaceJsonConverter<IBackupItem,BackupItem>(),
            new ObservableCollectionJsonConverter<IBackupItem>(),
            new ObservableCollectionJsonConverter<IRecentItem>()
        }
    };
    
    /// <summary>
    ///     Saves the specified settings to a configuration file
    /// </summary>
    /// <typeparam name="T">The type of settings to save</typeparam>
    /// <param name="settings">The settings to save</param>
    public void Save<T>(T settings)
    {
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, settings, jsonSerializerOptions);
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