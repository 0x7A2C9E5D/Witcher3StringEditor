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
    public Task<bool> Backup(string filePath);

    /// <summary>
    ///     Restores a file from the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item containing information about the backup to restore</param>
    /// <returns>True if the restore operation was successful, false otherwise</returns>
    public bool Restore(IBackupItem backupItem);

    /// <summary>
    ///     Deletes the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item to delete</param>
    /// <returns>True if the deletion was successful, false otherwise</returns>
    public bool Delete(IBackupItem backupItem);
}