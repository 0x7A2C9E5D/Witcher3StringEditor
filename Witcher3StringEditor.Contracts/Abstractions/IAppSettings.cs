using System.Collections.ObjectModel;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for application settings management
///     Provides access to user preferences, paths, and collections of recent and backup items
/// </summary>
public interface IAppSettings
{
    /// <summary>
    ///     Gets or sets the preferred The Witcher 3 file type for operations
    /// </summary>
    public W3FileType PreferredW3FileType { get; set; }

    /// <summary>
    ///     Gets or sets the preferred language for the application
    /// </summary>
    public W3Language PreferredLanguage { get; set; }

    /// <summary>
    ///     Gets or sets the path to the W3Strings tool executable
    /// </summary>
    public string W3StringsPath { get; set; }

    /// <summary>
    ///     Gets or sets the path to the game executable
    /// </summary>
    public string GameExePath { get; set; }

    /// <summary>
    ///     Gets the URL to the NexusMods page for this application
    /// </summary>
    public string NexusModUrl { get; }

    /// <summary>
    ///     Gets or sets the preferred translator service
    /// </summary>
    public string Translator { get; set; }

    /// <summary>
    ///     Gets the collection of recently opened items
    /// </summary>
    public ObservableCollection<IRecentItem> RecentItems { get; }

    /// <summary>
    ///     Gets the collection of backup items
    /// </summary>
    public ObservableCollection<IBackupItem> BackupItems { get; }

    /// <summary>
    ///     Gets or sets the application language
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    ///     Gets or sets the page size for pagination
    /// </summary>
    public int PageSize { get; set; }
}