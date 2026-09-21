using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Models;

/// <summary>
///     Represents a backup entry model
///     Implements the IBackupItem interface to provide concrete backup item information
///     This record stores metadata about a backed up file including its location, hash, and backup time
/// </summary>
internal record BackupItem : IBackupItem
{
    /// <summary>
    ///     Initializes a new instance of the BackupItem record and validates the backup metadata
    ///     Deserialization and construction both go through this constructor so that a corrupt settings file cannot
    ///     produce a backup item with a null or empty path
    /// </summary>
    /// <param name="fileName">The name of the backed up file</param>
    /// <param name="hash">The SHA-256 hash of the backed up file as a lower-case hexadecimal string</param>
    /// <param name="originalPath">The path of the file before it was backed up</param>
    /// <param name="backupPath">The path where the backup file is stored</param>
    /// <param name="backupTime">The time when the backup was created</param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="fileName" />, <paramref name="hash" />, <paramref name="originalPath" /> or
    ///     <paramref name="backupPath" /> is null, empty or consists only of white-space
    /// </exception>
    public BackupItem(string fileName, string hash, string originalPath, string backupPath, DateTime backupTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        FileName = fileName;
        Hash = hash;
        OrginPath = originalPath;
        BackupPath = backupPath;
        BackupTime = backupTime;
    }

    /// <summary>
    ///     Gets the name of the backed up file
    /// </summary>
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
    public DateTime BackupTime { get; }
}