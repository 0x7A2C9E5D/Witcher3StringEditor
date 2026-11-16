using System.Collections.ObjectModel;
using Akka.Actor;
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Bridge between the application settings collections and the Akka DistributedData replicas.
/// </summary>
internal interface ISharedRecordsService
{
    ObservableCollection<IRecentFileEntry> RecentItems { get; }

    ObservableCollection<IBackupItem> BackupItems { get; }

    SharedRecordsSnapshot CreateCurrentSnapshot();

    void AttachCoordinator(IActorRef coordinator);

    void UpsertRecentFile(string filePath, DateTime openedTime);

    void DeleteRecentFile(IRecentFileEntry entry);

    void UpsertBackup(IBackupItem item);

    void DeleteBackup(IBackupItem item);

    void ApplySnapshot(SharedRecordsSnapshot snapshot);

    void NotifyUpdateCompleted(string key);

    Task FlushAsync(TimeSpan timeout);
}