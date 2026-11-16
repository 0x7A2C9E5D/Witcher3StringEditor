using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Serilog;
using Witcher3StringEditor.Cluster;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Models;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Main-thread facade over the settings collections. Local writes update the collections
///     immediately and are handed to the publisher; replicated snapshots are materialized
///     idempotently on the UI thread.
/// </summary>
internal sealed class SharedRecordsService(IAppSettings appSettings, ISettingsPersistenceService settingsPersistence)
    : ISharedRecordsService
{
    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(2);
    private readonly Lock flushLock = new();
    private readonly HashSet<string> pendingKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SharedRecordVersion> tombstones = new(StringComparer.Ordinal);
    private ISharedRecordPublisher? recordPublisher;
    private TaskCompletionSource<bool>? flushCompletion;
    private string? lastSavedSignature;
    private int pendingWrites;
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
        var key = SharedRecordCodec.RecentKey(entry.FilePath);
        var value = SharedRecordCodec.EncodeDeletedRecent(entry.FilePath);
        RememberTombstone(key, SharedRecordCodec.GetVersion(false, value));
        Publish(new SharedRecordCommand(key, value, false));
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
        var key = SharedRecordCodec.BackupKey(item.BackupPath);
        var value = SharedRecordCodec.EncodeDeletedBackup(item);
        RememberTombstone(key, SharedRecordCodec.GetVersion(true, value));
        Publish(new SharedRecordCommand(key, value, true));
        ScheduleSave();
    }

    /// <summary>
    ///     Applies a record received from another instance. Deletions are remembered for the
    ///     session so a stale broadcast cannot resurrect a record that was already removed.
    /// </summary>
    public void TryApplyRemote(SharedRecordCommand command)
    {
        var applied = false;
        RunOnUiThread(() => applied = ApplyRemote(command));
        if (applied) ScheduleSave();
    }

    private bool ApplyRemote(SharedRecordCommand command)
    {
        var incoming = SharedRecordCodec.GetVersion(command.IsBackup, command.Value);
        if (IsTombstoned(command.Key, incoming)) return false;
        return command.IsBackup
            ? ApplyRemoteBackup(command, incoming)
            : ApplyRemoteRecent(command, incoming);
    }

    private bool ApplyRemoteRecent(SharedRecordCommand command, SharedRecordVersion incoming)
    {
        var payload = SharedRecordCodec.DecodeRecent(command.Value);
        if (payload is null) return false;

        var existing = RecentItems.FirstOrDefault(x =>
            string.Equals(SharedRecordCodec.RecentKey(x.FilePath), command.Key, StringComparison.Ordinal));

        if (existing is not null)
        {
            var current = SharedRecordCodec.GetVersion(false, SharedRecordCodec.EncodeRecent(existing));
            if (!incoming.Supersedes(current)) return false;

            if (payload.IsDeleted)
            {
                RecentItems.Remove(existing);
                RememberTombstone(command.Key, incoming);
                return true;
            }

            existing.OpenedTime = payload.OpenedTime;
            existing.IsMarked = payload.IsMarked;
            return true;
        }

        if (payload.IsDeleted)
        {
            RememberTombstone(command.Key, incoming);
            return false;
        }

        RecentItems.Add(new RecentItem(payload.FilePath, payload.OpenedTime, payload.IsMarked));
        return true;
    }

    private bool ApplyRemoteBackup(SharedRecordCommand command, SharedRecordVersion incoming)
    {
        var payload = SharedRecordCodec.DecodeBackup(command.Value);
        if (payload is null) return false;

        var existing = BackupItems.FirstOrDefault(x =>
            string.Equals(SharedRecordCodec.BackupKey(x.BackupPath), command.Key, StringComparison.Ordinal));

        if (existing is not null)
        {
            var current = SharedRecordCodec.GetVersion(true, SharedRecordCodec.EncodeBackup(existing));
            if (!incoming.Supersedes(current)) return false;

            if (payload.IsDeleted)
            {
                BackupItems.Remove(existing);
                RememberTombstone(command.Key, incoming);
                return true;
            }
        }
        else
        {
            if (payload.IsDeleted)
            {
                RememberTombstone(command.Key, incoming);
                return false;
            }

            BackupItems.Add(CreateBackupItem(payload));
            return true;
        }

        BackupItems[BackupItems.IndexOf(existing)] = CreateBackupItem(payload);
        return true;
    }

    private static BackupItem CreateBackupItem(BackupPayload payload)
    {
        return new BackupItem
        {
            FileName = payload.FileName,
            Hash = payload.Hash,
            OrginPath = payload.OriginPath,
            BackupPath = payload.BackupPath,
            BackupTime = payload.BackupTime
        };
    }

    private void RememberTombstone(string key, SharedRecordVersion version)
    {
        lock (tombstones)
        {
            tombstones[key] = version;
        }
    }

    private bool IsTombstoned(string key, SharedRecordVersion incoming)
    {
        lock (tombstones)
        {
            return tombstones.TryGetValue(key, out var tombstone) && !incoming.Supersedes(tombstone);
        }
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