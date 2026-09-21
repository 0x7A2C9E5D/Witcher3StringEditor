namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for backup service operations
///     Provides methods to create, restore, and delete file backups
/// </summary>
public interface IBackupService
{
    /// <summary>
    ///     Creates a backup of the specified file
    /// </summary>
    /// <param name="filePath">The path to the file to back up</param>
    /// <returns>True if the backup was created successfully, false otherwise</returns>
    /// <remarks>
    ///     Failures do not throw: the cause is logged and <c>false</c> is returned so that callers can
    ///     abort the operation that needed the backup
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="filePath" /> is null, empty or white-space</exception>
    Task<bool> BackupAsync(string filePath);

    /// <summary>
    ///     Restores a file from the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item containing information about the backup to restore</param>
    /// <returns>True if the restore operation was successful, false otherwise</returns>
    /// <remarks>
    ///     The backup content is verified against <see cref="IBackupItem.Hash" /> before the original file is
    ///     overwritten, so a stale or corrupted backup never destroys the current file content.
    ///     Failures do not throw: the cause is logged and <c>false</c> is returned
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="backupItem" /> is null</exception>
    Task<bool> RestoreAsync(IBackupItem backupItem);

    /// <summary>
    ///     Deletes the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item to delete</param>
    /// <returns>True if the deletion was successful, false otherwise</returns>
    /// <remarks>
    ///     Failures do not throw: the cause is logged and <c>false</c> is returned
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="backupItem" /> is null</exception>
    Task<bool> DeleteAsync(IBackupItem backupItem);
}