using System.IO;
using System.Security.Cryptography;
using CommunityToolkit.Diagnostics;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides backup functionality for files, with records replicated across instances.
/// </summary>
internal class BackupService(ISharedRecordsService sharedRecords) : IBackupService
{
    /// <summary>
    ///     Creates a backup of the specified file.
    /// </summary>
    public async Task<bool> Backup(string filePath)
    {
        try
        {
            var hash = await ValidateAndGetHash(filePath);
            var backupItem = new BackupItem
            {
                FileName = Path.GetFileName(filePath),
                Hash = hash,
                OrginPath = filePath,
                BackupPath = Path.Combine(AppPaths.BackupDirectory, $"{Guid.NewGuid():N}.bak"),
                BackupTime = DateTime.Now
            };
            Directory.CreateDirectory(AppPaths.BackupDirectory);
            if (!IsDuplicateBackup(backupItem)) return ExecuteBackup(backupItem);
            Log.Debug("Backup skipped, an identical backup already exists: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to backup file: {Path}", filePath);
            return false;
        }
    }

    /// <summary>
    ///     Restores a file from the specified backup item.
    /// </summary>
    public bool Restore(IBackupItem backupItem)
    {
        try
        {
            Guard.IsTrue(File.Exists(backupItem.BackupPath));
            var folder = Path.GetDirectoryName(backupItem.OrginPath);
            Guard.IsNotNullOrWhiteSpace(folder);
            Directory.CreateDirectory(folder);
            File.Copy(backupItem.BackupPath, backupItem.OrginPath, true);
            Log.Information("Restore backup file: {FileName}", backupItem.OrginPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restore backup item: {Path}", backupItem.OrginPath);
            return false;
        }
    }

    /// <summary>
    ///     Deletes the specified backup item and its backup file.
    /// </summary>
    public bool Delete(IBackupItem backupItem)
    {
        try
        {
            if (File.Exists(backupItem.BackupPath))
                File.Delete(backupItem.BackupPath);
            sharedRecords.DeleteBackup(backupItem);
            Log.Information("Delete backup file: {Path}", backupItem.BackupPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete backup item: {Path}", backupItem.BackupPath);
            return false;
        }
    }

    private static async Task<string> ValidateAndGetHash(string filePath)
    {
        Guard.IsTrue(File.Exists(filePath));
        var hash = await ComputeSha256Hash(filePath);
        Guard.IsNotNullOrWhiteSpace(hash);
        return hash;
    }

    private bool IsDuplicateBackup(BackupItem backupItem)
    {
        return sharedRecords.BackupItems.Any(x =>
            x.Hash == backupItem.Hash &&
            x.OrginPath == backupItem.OrginPath &&
            File.Exists(x.BackupPath));
    }

    private bool ExecuteBackup(BackupItem backupItem)
    {
        File.Copy(backupItem.OrginPath, backupItem.BackupPath);
        sharedRecords.UpsertBackup(backupItem);
        Log.Information("Backup file: {Path}", backupItem.OrginPath);
        return true;
    }

    private static async Task<string> ComputeSha256Hash(string filePath)
    {
        try
        {
            Guard.IsTrue(File.Exists(filePath));
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to compute SHA256 hash: {Path}", filePath);
            return string.Empty;
        }
    }
}