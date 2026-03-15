using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Messaging;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the backup dialog window
///     Handles backup restoration and deletion operations
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
/// <param name="appSettings">Application settings service</param>
/// <param name="backupService">Backup service for managing backup operations</param>
public partial class BackupDialogViewModel(
    IAppSettings appSettings,
    IBackupService backupService)
    : ObservableObject, IModalDialogViewModel
{
    /// <summary>
    ///     Gets the application settings service
    /// </summary>
    public IAppSettings AppSettings => appSettings;

    /// <summary>
    ///     Gets the dialog result value
    ///     Returns true to indicate that the dialog was closed successfully
    /// </summary>
    public bool? DialogResult => true;

    /// <summary>
    ///     Restores a backup file to its original location
    /// </summary>
    /// <param name="backupItem">The backup item to restore</param>
    [RelayCommand]
    private async Task Restore(IBackupItem backupItem)
    {
        if (!File.Exists(backupItem.BackupPath)) // Check if backup file exists
            await HandleMissingBackupFile(backupItem); // Handle missing backup file
        else
            await HandleExistingBackupFile(backupItem); // Handle existing backup file
    }

    /// <summary>
    ///     Handles the restoration process for an existing backup file
    ///     Sends a confirmation request and performs the restoration if approved
    /// </summary>
    /// <param name="backupItem">The backup item to restore</param>
    private async Task HandleExistingBackupFile(IBackupItem backupItem)
    {
        // Send a confirmation request
        if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(), MessageTokens.BackupRestore))
        {
            Log.Information("The restoration of file {Path} has been approved.", backupItem.OrginPath); // Log approval
            if (!backupService.Restore(backupItem)) // Attempt restoration
                _ = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                    MessageTokens.OperationFailed); // Notify if failed
        }
    }

    /// <summary>
    ///     Handles the case when a backup file is missing
    ///     Sends an error notification and deletes the backup item if confirmed
    /// </summary>
    /// <param name="backupItem">The backup item with the missing file</param>
    private async Task HandleMissingBackupFile(IBackupItem backupItem)
    {
        Log.Error("The backup file {Path} does no exist.", backupItem.BackupPath); // Log error
        if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(), MessageTokens.BackupFileNoFound))
            backupService.Delete(backupItem); // Delete the backup item if confirmed
    }

    /// <summary>
    ///     Deletes a backup file
    /// </summary>
    /// <param name="backupItem">The backup item to delete</param>
    [RelayCommand]
    private async Task Delete(IBackupItem backupItem)
    {
        if (await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(),
                MessageTokens.BackupDelete)) // Confirm deletion
        {
            Log.Information("The deletion of file {Path} has been approved.", backupItem.BackupPath); // Log approval
            if (!backupService.Delete(backupItem)) // Attempt deletion
                _ = await WeakReferenceMessenger.Default.Send(new AsyncRequestMessage<bool>(), // Notify if failed
                    MessageTokens.OperationFailed);
        }
    }
}