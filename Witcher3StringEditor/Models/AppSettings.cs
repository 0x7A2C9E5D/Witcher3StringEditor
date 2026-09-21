using System.Collections.ObjectModel;
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
    ///     The page size used when the persisted value is missing or invalid
    /// </summary>
    private const int DefaultPageSize = 30;

    /// <summary>
    ///     Gets or sets the path to the game executable
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string gameExePath = string.Empty;

    /// <summary>
    ///     Gets or sets the application language
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string language = string.Empty;

    /// <summary>
    ///     Gets or sets the page size for pagination
    ///     Must be a positive integer; a non-positive persisted value is coerced back to <see cref="DefaultPageSize" />
    /// </summary>
    [ObservableProperty] private int pageSize = DefaultPageSize;

    /// <summary>
    ///     Gets or sets the preferred language for The Witcher 3 string operations
    ///     Initialized explicitly to <see cref="W3Language.En" /> because <c>default(W3Language)</c> is Arabic
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private W3Language preferredLanguage = W3Language.En;

    /// <summary>
    ///     Gets or sets the preferred The Witcher 3 file type for operations
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private W3FileType preferredW3FileType;

    /// <summary>
    ///     Gets or sets the preferred translator service
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string translator = "MicrosoftTranslator";

    /// <summary>
    ///     Gets or sets the path to the W3Strings tool executable
    ///     This property supports data binding through the ObservableObject base class
    /// </summary>
    [ObservableProperty] private string w3StringsPath = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the AppSettings class with specified values
    ///     This constructor is used during JSON deserialization. Every parameter is optional so that a
    ///     configuration file missing properties (or containing explicit nulls) still produces usable settings
    /// </summary>
    /// <param name="w3StringsPath">The path to the W3Strings tool executable</param>
    /// <param name="gameExePath">The path to the game executable</param>
    /// <param name="preferredW3FileType">The preferred The Witcher 3 file type</param>
    /// <param name="preferredLanguage">The preferred language</param>
    /// <param name="backupItems">The collection of backup items, may be null in a persisted configuration file</param>
    /// <param name="recentItems">The collection of recent items, may be null in a persisted configuration file</param>
    [JsonConstructor]
    public AppSettings(string w3StringsPath = "", string gameExePath = "",
        W3FileType preferredW3FileType = W3FileType.Csv, W3Language preferredLanguage = W3Language.En,
        ObservableCollection<IBackupItem>? backupItems = null,
        ObservableCollection<IRecentFileEntry>? recentItems = null)
    {
        GameExePath = gameExePath;
        W3StringsPath = w3StringsPath;
        BackupItems = backupItems ?? [];
        RecentItems = recentItems ?? [];
        PreferredW3FileType = preferredW3FileType;
        PreferredLanguage = preferredLanguage;
    }

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
    public ObservableCollection<IRecentFileEntry> RecentItems { get; }

    /// <summary>
    ///     Gets the collection of backup items
    ///     This collection supports data binding through the ObservableObject base class
    /// </summary>
    public ObservableCollection<IBackupItem> BackupItems { get; }

    /// <summary>
    ///     Coerces an invalid persisted page size back to the default value
    /// </summary>
    /// <param name="value">The new page size</param>
    partial void OnPageSizeChanged(int value)
    {
        if (value <= 0) PageSize = DefaultPageSize;
    }
}