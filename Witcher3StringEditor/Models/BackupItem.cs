using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents a backup item model
///     Implements the IBackupItem interface to provide concrete backup item information
///     This record stores metadata about a backed up file including its location, hash, and backup time
/// </summary>
internal record BackupItem : IBackupItem
{
    /// <summary>
    ///     Gets the name of the backed up file
    ///     This property is required during record initialization
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    ///     Gets the hash of the backed up file
    ///     Used to verify file integrity and detect changes
    ///     This property is required during record initialization
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    ///     Gets the original path of the file before backup
    ///     This property is required during record initialization
    /// </summary>
    public required string OrginPath { get; init; }

    /// <summary>
    ///     Gets the path where the backup file is stored
    ///     This property is required during record initialization
    /// </summary>
    public required string BackupPath { get; init; }

    /// <summary>
    ///     Gets the time when the backup was created
    ///     This property is required during record initialization
    /// </summary>
    public required DateTime BackupTime { get; init; }
}