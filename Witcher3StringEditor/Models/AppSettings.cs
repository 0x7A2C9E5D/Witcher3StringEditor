using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents the application settings model
///     Implements the IAppSettings interface and provides observable properties for data binding
///     This class is used to store and manage application-level settings and preferences
/// </summary>
internal partial class AppSettings : ObservableObject, IAppSettings
{
    /// <summary>
    ///     Initializes a new instance of the AppSettings class
    ///     Creates an empty settings object with default values
    /// </summary>
    public AppSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the AppSettings class with specified values
    ///     This constructor is used during JSON deserialization
    /// </summary>
    /// <param name="gameExePath">The path to the game executable</param>
    /// <param name="preferredW3FileType">The preferred The Witcher 3 file format</param>
    /// <param name="preferredLanguage">The preferred language</param>
    /// <param name="backupItems">The collection of backup items</param>
    /// <param name="recentItems">The collection of recent items</param>
    [JsonConstructor]
    public AppSettings(string gameExePath, W3FileFormat preferredW3FileType,
        W3Language? preferredLanguage, ObservableCollection<IBackupItem> backupItems,
        ObservableCollection<IRecentFileEntry> recentItems)
    {
        GameExePath = gameExePath;
        BackupItems = [.. backupItems];
        RecentItems = [.. recentItems];
        PreferredW3FileType = preferredW3FileType;
        PreferredLanguage = preferredLanguage ?? W3Language.Default; // Settings may predate the language
    }

    /// <summary>
    ///     Gets or sets the path to the game executable
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial string GameExePath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the application language
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the page size for pagination
    /// </summary>
    [ObservableProperty]
    public partial int PageSize { get; set; } = 30;

    /// <summary>
    ///     Gets or sets the preferred language for The Witcher 3 string operations
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial W3Language PreferredLanguage { get; set; }

    /// <summary>
    ///     Gets or sets the preferred payload encoding written for .w3strings files
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial Encoding PreferredW3StringsEncoding { get; set; } = Encoding.Unicode;

    /// <summary>
    ///     Gets or sets the preferred The Witcher 3 file format for operations
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial W3FileFormat PreferredW3FileType { get; set; }

    /// <summary>
    ///     Gets or sets the preferred translator service
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty]
    public partial string Translator { get; set; } = "MicrosoftTranslator";

    /// <summary>
    ///     Gets the URL to the NexusMods page for this application
    ///     This property is ignored during JSON serialization
    /// </summary>
    [JsonIgnore]
    public string NexusModUrl => "https://www.nexusmods.com/witcher3/mods/10032";

    /// <summary>
    ///     Gets the collection of recently opened items
    ///     This collection supports data binding through the ObservableObject base class
    /// </summary>
    public ObservableCollection<IRecentFileEntry> RecentItems { get; } = [];

    /// <summary>
    ///     Gets the collection of backup items
    ///     This collection supports data binding through the ObservableObject base class
    /// </summary>
    public ObservableCollection<IBackupItem> BackupItems { get; } = [];
}