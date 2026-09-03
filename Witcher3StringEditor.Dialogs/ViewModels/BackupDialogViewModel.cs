using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Locales;
using Witcher3StringEditor.Shared.Extensions;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     ViewModel for the backup dialog window
///     Handles backup restoration and deletion operations
///     Implements IModalDialogViewModel to support dialog result handling
/// </summary>
/// <param name="appSettings">Application settings service</param>
/// <param name="backupService">Backup service for managing backup operations</param>
/// <param name="dialogService">Dialog service used to inform or question the user</param>
public partial class BackupDialogViewModel(
    IAppSettings appSettings,
    IBackupService backupService,
    IDialogService dialogService)
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
    ///     Asks for confirmation and performs the restoration if approved
    /// </summary>
    /// <param name="backupItem">The backup item to restore</param>
    private async Task HandleExistingBackupFile(IBackupItem backupItem)
    {
        // Ask for confirmation and restore the backup if approved
        if (await dialogService.MessageBoxConfirmAsync(this, Strings.BackupRestoreMessage,
                Strings.BackupRestoreCaption) &&
            !backupService.Restore(backupItem)) // Attempt restoration
            await dialogService.MessageBoxNotifyAsync(this, Strings.OperationFailureMessage,
                Strings.OperationResultCaption, MessageBoxIcon.Warning); // Notify if failed
    }

    /// <summary>
    ///     Handles the case when a backup file is missing
    ///     Notifies the user and deletes the backup item if confirmed
    /// </summary>
    /// <param name="backupItem">The backup item with the missing file</param>
    private async Task HandleMissingBackupFile(IBackupItem backupItem)
    {
        Log.Warning("The backup file {Path} does not exist.", backupItem.BackupPath); // Log warning
        if (await dialogService.MessageBoxConfirmAsync(this, Strings.BackupFileNoFoundMessage,
                Strings.BackupFileNoFoundCaption))
            backupService.Delete(backupItem); // Delete the backup item if confirmed
    }

    /// <summary>
    ///     Deletes a backup file
    /// </summary>
    /// <param name="backupItem">The backup item to delete</param>
    [RelayCommand]
    private async Task Delete(IBackupItem backupItem)
    {
        // Confirm deletion and delete the backup if approved
        if (await dialogService.MessageBoxConfirmAsync(this, Strings.BackupDeleteMessage,
                Strings.BackupDeleteCaption) && !backupService.Delete(backupItem)) // Attempt deletion
            await dialogService.MessageBoxNotifyAsync(this, Strings.OperationFailureMessage,
                Strings.OperationResultCaption, MessageBoxIcon.Warning); // Notify if failed
    }
}
