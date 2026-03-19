using System.IO;
using System.Security.Cryptography;
using CommunityToolkit.Diagnostics;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Helpers;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides backup functionality for files
///     Implements the IBackupService interface to handle creating, restoring, and deleting file backups
/// </summary>
internal class BackupService(IAppSettings appSettings) : IBackupService
{
    /// <summary>
    ///     Creates a backup of the specified file
    /// </summary>
    /// <param name="filePath">The path to the file to back up</param>
    /// <returns>True if the backup was created successfully, false otherwise</returns>
    public async Task<bool> Backup(string filePath)
    {
        try
        {
            var hash = await ValidateAndGetHash(filePath); // Validate file and compute hash
            var backupItem = new BackupItem // Create new backup item
            {
                FileName = Path.GetFileName(filePath), // Set file name
                Hash = hash, // Set file hash
                OrginPath = filePath, // Set original file path
                BackupPath =
                    Path.Combine(PathHelper.BackupDirectory, $"{Guid.NewGuid():N}.bak"), // Set backup file path
                BackupTime = DateTime.Now // Set backup time
            };
            EnsureBackupDirectoryExists(PathHelper.BackupDirectory); // Ensure backup directory exists
            return IsDuplicateBackup(backupItem) || ExecuteBackup(backupItem); // Check for duplicates or execute backup
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to backup file: {Path}.", filePath); // Log any errors
            return false; // Return false on failure
        }
    }

    /// <summary>
    ///     Restores a file from the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item containing information about the backup to restore</param>
    /// <returns>True if the restore operation was successful, false otherwise</returns>
    public bool Restore(IBackupItem backupItem)
    {
        try
        {
            Guard.IsTrue(File.Exists(backupItem.BackupPath)); // Ensure backup file exists
            var folder = Path.GetDirectoryName(backupItem.OrginPath); // Get directory of original file
            Guard.IsNotNullOrWhiteSpace(folder); // Ensure folder path is valid
            if (!Directory.Exists(folder)) // Create directory if it doesn't exist
                Directory.CreateDirectory(folder);
            File.Copy(backupItem.BackupPath, backupItem.OrginPath, true); // Copy backup to original location
            Log.Information("Restore backup file: {FileName}.", backupItem.OrginPath); // Log successful restore
            return true; // Return true on success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restore backup item: {Path}.", backupItem.OrginPath); // Log any errors
            return false; // Return false on failure
        }
    }

    /// <summary>
    ///     Deletes the specified backup item
    /// </summary>
    /// <param name="backupItem">The backup item to delete</param>
    /// <returns>True if the deletion was successful, false otherwise</returns>
    public bool Delete(IBackupItem backupItem)
    {
        try
        {
            if (File.Exists(backupItem.BackupPath)) // Check if backup file exists
                File.Delete(backupItem.BackupPath); // Delete the backup file
            appSettings.BackupItems.Remove(backupItem); // Remove from backup items collection
            Log.Information("Delete backup file: {Path}.", backupItem.BackupPath); // Log successful deletion
            return true; // Return true on success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete backup item: {Path}.", backupItem.BackupPath); // Log any errors
            return false; // Return false on failure
        }
    }

    /// <summary>
    ///     Validates that the file exists and computes its SHA256 hash
    /// </summary>
    /// <param name="filePath">The path to the file to validate and hash</param>
    /// <returns>The SHA256 hash of the file</returns>
    private static async Task<string> ValidateAndGetHash(string filePath)
    {
        Guard.IsTrue(File.Exists(filePath)); // Ensure file exists
        var hash = await ComputeSha256Hash(filePath); // Compute SHA256 hash of the file
        Guard.IsNotNullOrWhiteSpace(hash); // Ensure hash is not null or whitespace
        return hash; // Return the computed hash
    }

    /// <summary>
    ///     Ensures that the backup directory exists, creating it if necessary
    /// </summary>
    /// <param name="backupDirectoryPath">The path to the backup directory</param>
    private static void EnsureBackupDirectoryExists(string backupDirectoryPath)
    {
        if (!Directory.Exists(backupDirectoryPath)) // Check if backup directory exists
            Directory.CreateDirectory(backupDirectoryPath); // Create directory if it doesn't exist
    }

    /// <summary>
    ///     Checks if a backup with the same hash and original path already exists
    /// </summary>
    /// <param name="backupItem">The backup item to check for duplicates</param>
    /// <returns>True if a duplicate backup exists, false otherwise</returns>
    private bool IsDuplicateBackup(BackupItem backupItem)
    {
        return appSettings.BackupItems.Any(x => // Check if any existing backup item matches
            x.Hash == backupItem.Hash && // Same hash
            x.OrginPath == backupItem.OrginPath && // Same original path
            File.Exists(x.BackupPath)); // Backup file still exists
    }

    /// <summary>
    ///     Executes the backup operation by copying the file to the backup location and adding it to the backup items
    ///     collection
    /// </summary>
    /// <param name="backupItem">The backup item to execute the backup for</param>
    /// <returns>True if the backup was executed successfully</returns>
    private bool ExecuteBackup(BackupItem backupItem)
    {
        File.Copy(backupItem.OrginPath, backupItem.BackupPath); // Copy file to back up location
        appSettings.BackupItems.Add(backupItem); // Add backup item to collection
        Log.Information("Backup file: {Path}.", backupItem.OrginPath); // Log successful backup
        return true; // Return true on success
    }

    /// <summary>
    ///     Computes the SHA256 hash of the specified file
    /// </summary>
    /// <param name="filePath">The path to the file to hash</param>
    /// <returns>The SHA256 hash of the file as a hexadecimal string</returns>
    private static async Task<string> ComputeSha256Hash(string filePath)
    {
        try
        {
            Guard.IsTrue(File.Exists(filePath)); // Ensure file exists
            using var sha256 = SHA256.Create(); // Create SHA256 hasher
            await using var stream = File.OpenRead(filePath); // Open file for reading
            var hash = await sha256.ComputeHashAsync(stream);
#if NET10_0
            return Convert.ToHexStringLower(hash); // Compute and format hash
#else
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); // Compute and format hash
#endif
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to compute SHA256 hash: {Path}.", filePath); // Log any errors
            return string.Empty; // Return empty string on failure
        }
    }
}