using System.Collections.ObjectModel;
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Bridge between the application settings collections and the replicated shared record log.
/// </summary>
internal interface ISharedRecordsService
{
    ObservableCollection<IRecentFileEntry> RecentItems { get; }

    ObservableCollection<IBackupItem> BackupItems { get; }

    SharedRecordsSnapshot CreateCurrentSnapshot();

    void AttachPublisher(ISharedRecordPublisher publisher);

    /// <summary>
    ///     Supplies the probe that tells whether this instance owns the shared record part of the
    ///     settings file. Only the Raft leader may write the replicated collections to disk.
    /// </summary>
    void AttachWriteAuthority(Func<bool> isLeader);

    void UpsertRecentFile(string filePath, DateTime openedTime);

    void DeleteRecentFile(IRecentFileEntry entry);

    void UpsertBackup(IBackupItem item);

    void DeleteBackup(IBackupItem item);

    void ApplySnapshot(SharedRecordsSnapshot snapshot);

    void NotifyUpdateCompleted(string key);

    Task FlushAsync(TimeSpan timeout);
}