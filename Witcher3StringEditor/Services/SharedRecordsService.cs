using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Akka.Actor;
using Serilog;
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Main-thread facade over the settings collections. Local writes update the collections
///     immediately and are sent to the coordinator actor; remote snapshots are materialized
///     idempotently on the UI thread.
/// </summary>
internal sealed class SharedRecordsService(IAppSettings appSettings, ISettingsPersistenceService settingsPersistence)
    : ISharedRecordsService
{
    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(2);
    private readonly Lock flushLock = new();
    private readonly HashSet<string> pendingKeys = new(StringComparer.Ordinal);
    private IActorRef? coordinator;
    private TaskCompletionSource<bool>? flushCompletion;
    private string? lastSavedSignature;
    private int pendingWrites;
    private CancellationTokenSource? saveDebounceSource;

    public ObservableCollection<IRecentFileEntry> RecentItems => appSettings.RecentItems;

    public ObservableCollection<IBackupItem> BackupItems => appSettings.BackupItems;

    public void AttachCoordinator(IActorRef newCoordinator)
    {
        Volatile.Write(ref coordinator, newCoordinator);
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
        Publish(SharedRecordCodec.RecentKey(filePath),
            new UpsertSharedRecord(SharedRecordCodec.RecentKey(filePath), value, false));
        ScheduleSave();
    }

    public void DeleteRecentFile(IRecentFileEntry entry)
    {
        RunOnUiThread(() => RecentItems.Remove(entry));
        Publish(SharedRecordCodec.RecentKey(entry.FilePath),
            new DeleteSharedRecord(
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
        Publish(SharedRecordCodec.BackupKey(item.BackupPath),
            new UpsertSharedRecord(
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
        Publish(SharedRecordCodec.BackupKey(item.BackupPath),
            new DeleteSharedRecord(
                SharedRecordCodec.BackupKey(item.BackupPath),
                SharedRecordCodec.EncodeDeletedBackup(item), true));
        ScheduleSave();
    }

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
            pendingKeys.Remove(key);
        }

        var remaining = Interlocked.Decrement(ref pendingWrites);
        switch (remaining)
        {
            case < 0:
                Interlocked.Exchange(ref pendingWrites, 0);
                break;
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

    private void Publish(string key, object message)
    {
        var target = Volatile.Read(ref coordinator);
        if (target is null) return;
        lock (pendingKeys)
        {
            pendingKeys.Add(key);
        }

        Interlocked.Increment(ref pendingWrites);
        target.Tell(message);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    /// <summary>
    ///     Keys with a local write not yet acknowledged by the replicator. A remote snapshot
    ///     that temporarily misses such a key must not remove it from the UI collections.
    /// </summary>
    private bool IsPending(string key)
    {
        lock (pendingKeys)
        {
            return pendingKeys.Contains(key);
        }
    }

    private void MaterializeRecent(IReadOnlyDictionary<string, string> snapshot)
    {
        for (var i = RecentItems.Count - 1; i >= 0; i--)
        {
            var key = SharedRecordCodec.RecentKey(RecentItems[i].FilePath);
            if (!snapshot.ContainsKey(key) && !IsPending(key))
                RecentItems.RemoveAt(i);
        }

        foreach (var pair in snapshot)
        {
            var payload = SharedRecordCodec.DecodeRecent(pair.Value);
            var existing = RecentItems.FirstOrDefault(x =>
                string.Equals(SharedRecordCodec.RecentKey(x.FilePath), pair.Key, StringComparison.Ordinal));
            if (payload is null || payload.IsDeleted)
            {
                if (existing is not null) RecentItems.Remove(existing);
                continue;
            }

            if (existing is null)
                RecentItems.Add(new RecentItem(payload.FilePath, payload.OpenedTime, payload.IsMarked));
            else if (existing.OpenedTime != payload.OpenedTime) existing.OpenedTime = payload.OpenedTime;
        }
    }

    private void MaterializeBackups(IReadOnlyDictionary<string, string> snapshot)
    {
        for (var i = BackupItems.Count - 1; i >= 0; i--)
        {
            var key = SharedRecordCodec.BackupKey(BackupItems[i].BackupPath);
            if (!snapshot.ContainsKey(key) && !IsPending(key))
                BackupItems.RemoveAt(i);
        }

        foreach (var pair in snapshot)
        {
            var payload = SharedRecordCodec.DecodeBackup(pair.Value);
            var existing = BackupItems.FirstOrDefault(x =>
                string.Equals(SharedRecordCodec.BackupKey(x.BackupPath), pair.Key, StringComparison.Ordinal));
            if (payload is null || payload.IsDeleted)
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
                BackupItems.Add(item);
            else if (existing.BackupTime != item.BackupTime || existing.Hash != item.Hash)
                BackupItems[BackupItems.IndexOf(existing)] = item;
        }
    }
}