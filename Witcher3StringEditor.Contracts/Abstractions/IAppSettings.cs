using System.Collections.ObjectModel;
using System.Text;
using Witcher3StringEditor.W3Strings;

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
    ///     Gets or sets the preferred payload encoding written for <c>.w3strings</c> files
    /// </summary>
    public Encoding PreferredW3StringsEncoding { get; set; }

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
    public ObservableCollection<IRecentFileEntry> RecentItems { get; }

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