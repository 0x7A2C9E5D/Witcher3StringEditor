using JetBrains.Annotations;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for backup item information
///     Represents metadata about a backed up file including its location, hash, and backup time
/// </summary>
public interface IBackupItem
{
    /// <summary>
    ///     Gets the name of the backed up file
    /// </summary>
    [UsedImplicitly]
    public string FileName { get; }

    /// <summary>
    ///     Gets the hash of the backed up file
    ///     Used to verify file integrity and detect changes
    /// </summary>
    public string Hash { get; }

    /// <summary>
    ///     Gets the original path of the file before backup
    /// </summary>
    public string OrginPath { get; }

    /// <summary>
    ///     Gets the path where the backup file is stored
    /// </summary>
    public string BackupPath { get; }

    /// <summary>
    ///     Gets the time when the backup was created
    /// </summary>
    [UsedImplicitly]
    public DateTime BackupTime { get; }
}