using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Serilog;
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts;
using Witcher3StringEditor.Models;

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

    void UpsertRecentFile(string filePath, DateTime openedTime);

    void DeleteRecentFile(IRecentFileEntry entry);

    void UpsertBackup(IBackupItem item);

    void DeleteBackup(IBackupItem item);

    void ApplySnapshot(SharedRecordsSnapshot snapshot);

    void NotifyUpdateCompleted(string key);

    Task FlushAsync(TimeSpan timeout);
}

/// <summary>
///     Main-thread facade over the settings collections. Local writes update the collections
///     immediately and are handed to the publisher; replicated snapshots are materialized
///     idempotently on the UI thread.
/// </summary>
internal sealed class SharedRecordsService(
    IAppSettings appSettings,
    ISettingsPersistenceService settingsPersistence,
    IClusterLeadership leadership)
    : ISharedRecordsService
{
    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(2);
    private readonly Lock flushLock = new();
    private readonly HashSet<string> pendingKeys = new(StringComparer.Ordinal);
    private TaskCompletionSource<bool>? flushCompletion;
    private string? lastSavedSignature;
    private int pendingWrites;
    private ISharedRecordPublisher? recordPublisher;
    private CancellationTokenSource? saveDebounceSource;

    public ObservableCollection<IRecentFileEntry> RecentItems => appSettings.RecentItems;

    public ObservableCollection<IBackupItem> BackupItems => appSettings.BackupItems;

    public void AttachPublisher(ISharedRecordPublisher publisher)
    {
        Volatile.Write(ref recordPublisher, publisher);
    }

    public SharedRecordsSnapshot CreateCurrentSnapshot()
    {
        var recent = new Dictionary<string, string>(StringComparer.Ordinal);
        var backups = new Dictionary<string, string>(StringComparer.Ordinal);
        RunOnUiThread(() =>
        {
            foreach (var item in RecentItems)
                recent[SharedRecordCodec.RecentKey(item.FilePath)] =
                    SharedRecordCodec.EncodeRecent(item);
            foreach (var item in BackupItems)
                backups[SharedRecordCodec.BackupKey(item.BackupPath)] =
                    SharedRecordCodec.EncodeBackup(item);
        });
        return new SharedRecordsSnapshot(recent, backups);
    }

    public void UpsertRecentFile(string filePath, DateTime openedTime)
    {
        var value = string.Empty;
        RunOnUiThread(() =>
        {
            var existing = RecentItems.FirstOrDefault(x =>
                string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.OpenedTime = openedTime;
                value = SharedRecordCodec.EncodeRecent(existing);
            }
            else
            {
                var item = new RecentItem(filePath, openedTime);
                RecentItems.Add(item);
                value = SharedRecordCodec.EncodeRecent(item);
            }
        });
        Publish(new SharedRecordCommand(
            SharedRecordCodec.RecentKey(filePath), value, false));
        ScheduleSave();
    }

    public void DeleteRecentFile(IRecentFileEntry entry)
    {
        RunOnUiThread(() => RecentItems.Remove(entry));
        Publish(new SharedRecordCommand(
            SharedRecordCodec.RecentKey(entry.FilePath),
            SharedRecordCodec.EncodeDeletedRecent(entry.FilePath), false));
        ScheduleSave();
    }

    public void UpsertBackup(IBackupItem item)
    {
        RunOnUiThread(() =>
        {
            var existing = BackupItems.FirstOrDefault(x =>
                string.Equals(x.BackupPath, item.BackupPath, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                var index = BackupItems.IndexOf(existing);
                BackupItems[index] = item;
            }
            else
            {
                BackupItems.Add(item);
            }
        });
        Publish(new SharedRecordCommand(
            SharedRecordCodec.BackupKey(item.BackupPath),
            SharedRecordCodec.EncodeBackup(item), true));
        ScheduleSave();
    }

    public void DeleteBackup(IBackupItem item)
    {
        RunOnUiThread(() =>
        {
            var existing = BackupItems.FirstOrDefault(x =>
                string.Equals(x.BackupPath, item.BackupPath, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) BackupItems.Remove(existing);
        });
        Publish(new SharedRecordCommand(
            SharedRecordCodec.BackupKey(item.BackupPath),
            SharedRecordCodec.EncodeDeletedBackup(item), true));
        ScheduleSave();
    }

    /// <summary>
    ///     Materializes the replicated state on the UI thread. Keys with a local write that has
    ///     not reached the log yet are kept, because the snapshot may still be one step behind.
    /// </summary>
    public void ApplySnapshot(SharedRecordsSnapshot snapshot)
    {
        RunOnUiThread(() =>
        {
            MaterializeRecent(snapshot.RecentFiles);
            MaterializeBackups(snapshot.Backups);
        });
        ScheduleSave();
    }

    public void NotifyUpdateCompleted(string key)
    {
        lock (pendingKeys)
        {
            if (!pendingKeys.Remove(key)) return;
        }

        var remaining = Interlocked.Decrement(ref pendingWrites);
        switch (remaining)
        {
            case < 0:
                Interlocked.Exchange(ref pendingWrites, 0);
                return;
            case > 0:
                return;
        }

        lock (flushLock)
        {
            flushCompletion?.TrySetResult(true);
        }
    }

    public Task FlushAsync(TimeSpan timeout)
    {
        if (Volatile.Read(ref pendingWrites) <= 0) return Task.CompletedTask;
        TaskCompletionSource<bool> completion;
        lock (flushLock)
        {
            if (pendingWrites <= 0) return Task.CompletedTask;
            flushCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = flushCompletion;
        }

        return completion.Task.WaitAsync(timeout, CancellationToken.None);
    }

    /// <summary>
    ///     Applies one replicated recent file. A key that is missing from the snapshot is left
    ///     untouched: the replicated state keeps tombstones forever, so an absent key means this
    ///     instance simply has not received it yet, not that the record was deleted.
    /// </summary>
    private void MaterializeRecent(IReadOnlyDictionary<string, string> snapshot)
    {
        foreach (var pair in snapshot)
        {
            var payload = SharedRecordCodec.DecodeRecent(pair.Value);
            if (payload is null) continue;

            var existing = RecentItems.FirstOrDefault(x =>
                string.Equals(SharedRecordCodec.RecentKey(x.FilePath), pair.Key, StringComparison.Ordinal));
            if (payload.IsDeleted)
            {
                if (existing is not null) RecentItems.Remove(existing);
                continue;
            }

            if (existing is null)
            {
                RecentItems.Add(new RecentItem(payload.FilePath, payload.OpenedTime, payload.IsMarked));
                continue;
            }

            // Keep a newer local value that has not reached the log yet.
            var incoming = SharedRecordCodec.GetVersion(false, pair.Value);
            var current = SharedRecordCodec.GetVersion(false, SharedRecordCodec.EncodeRecent(existing));
            if (!incoming.Supersedes(current)) continue;

            existing.OpenedTime = payload.OpenedTime;
            existing.IsMarked = payload.IsMarked;
        }
    }

    private void MaterializeBackups(IReadOnlyDictionary<string, string> snapshot)
    {
        foreach (var pair in snapshot)
        {
            var payload = SharedRecordCodec.DecodeBackup(pair.Value);
            if (payload is null) continue;

            var existing = BackupItems.FirstOrDefault(x =>
                string.Equals(SharedRecordCodec.BackupKey(x.BackupPath), pair.Key, StringComparison.Ordinal));
            if (payload.IsDeleted)
            {
                if (existing is not null) BackupItems.Remove(existing);
                continue;
            }

            var item = new BackupItem
            {
                FileName = payload.FileName,
                Hash = payload.Hash,
                OrginPath = payload.OriginPath,
                BackupPath = payload.BackupPath,
                BackupTime = payload.BackupTime
            };
            if (existing is null)
            {
                BackupItems.Add(item);
                continue;
            }

            // Keep a newer local value that has not reached the log yet.
            var incoming = SharedRecordCodec.GetVersion(true, pair.Value);
            var current = SharedRecordCodec.GetVersion(true, SharedRecordCodec.EncodeBackup(existing));
            if (!incoming.Supersedes(current)) continue;

            BackupItems[BackupItems.IndexOf(existing)] = item;
        }
    }

    /// <summary>
    ///     Schedules a debounced settings save so record changes reach AppSettings.json while
    ///     the application is running instead of only when the last instance exits.
    /// </summary>
    private void ScheduleSave()
    {
        var source = new CancellationTokenSource();
        Interlocked.Exchange(ref saveDebounceSource, source)?.Cancel();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SaveDebounce, source.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            SaveSettings();
        }, source.Token);
    }

    private void SaveSettings()
    {
        if (!leadership.IsLeader) return; // A follower never writes the shared settings file
        try
        {
            RunOnUiThread(() =>
            {
                var signature = CreateSignature();
                if (string.Equals(signature, lastSavedSignature, StringComparison.Ordinal)) return;
                settingsPersistence.Save(appSettings);
                lastSavedSignature = signature;
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to persist shared records");
        }
    }

    private string CreateSignature()
    {
        var builder = new StringBuilder();
        foreach (var item in RecentItems.OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase))
            builder.Append(item.FilePath).Append('|').Append(item.OpenedTime.Ticks).Append(';');
        builder.Append('#');
        foreach (var item in BackupItems.OrderBy(x => x.BackupPath, StringComparer.OrdinalIgnoreCase))
            builder.Append(item.BackupPath).Append('|').Append(item.Hash).Append('|')
                .Append(item.BackupTime.Ticks).Append(';');
        return builder.ToString();
    }

    private void Publish(SharedRecordCommand command)
    {
        var target = Volatile.Read(ref recordPublisher);
        if (target is null) return;

        lock (pendingKeys)
        {
            if (pendingKeys.Add(command.Key)) Interlocked.Increment(ref pendingWrites);
        }

        target.Publish(command);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }
}