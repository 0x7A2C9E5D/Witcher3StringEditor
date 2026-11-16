using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData;
using Serilog;
using static Akka.DistributedData.Dsl;
using ReadConsistency = Akka.DistributedData.ReadLocal;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Akka actor that owns the two in-memory LWW maps. It seeds records loaded from
///     AppSettings.json, publishes local writes, and materializes remote changes back
///     into the shared state on the UI thread.
/// </summary>
internal sealed class SharedRecordsCoordinator : ReceiveActor
{
    private static readonly LWWDictionaryKey<string, string> RecentMapKey = new("witcher3-recent-files");
    private static readonly LWWDictionaryKey<string, string> BackupMapKey = new("witcher3-backups");
    private static readonly Clock<string> RecentClock = SharedRecordCodec.RecentClock;
    private static readonly Clock<string> BackupClock = SharedRecordCodec.BackupClock;

    // Waits until the write is known by every live member, so a flush before exit
    // propagates deletions instead of leaving the last quitter with stale records.
    private static readonly IWriteConsistency WriteAll = new WriteAll(TimeSpan.FromSeconds(2));
    private readonly Dictionary<string, string> backups = new(StringComparer.Ordinal);
    private readonly Cluster cluster;
    private readonly Dictionary<string, string> pendingBackupSeeds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> pendingRecentSeeds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> recent = new(StringComparer.Ordinal);

    private readonly IActorRef replicator;
    private readonly ISharedRecordsState state;
    private int initializationCount;
    private bool initialized;

    public SharedRecordsCoordinator(
        ISharedRecordsState state,
        IActorRef replicator,
        SharedRecordsSnapshot initialSnapshot)
    {
        this.state = state;
        this.replicator = replicator;
        cluster = Cluster.Get(Context.System);

        foreach (var pair in initialSnapshot.RecentFiles)
            pendingRecentSeeds[pair.Key] = pair.Value;
        foreach (var pair in initialSnapshot.Backups)
            pendingBackupSeeds[pair.Key] = pair.Value;

        replicator.Tell(Subscribe(RecentMapKey, Self));
        replicator.Tell(Subscribe(BackupMapKey, Self));
        replicator.Ask<IGetResponse>(Get(RecentMapKey, ReadConsistency.Instance)).PipeTo(Self);
        replicator.Ask<IGetResponse>(Get(BackupMapKey, ReadConsistency.Instance)).PipeTo(Self);

        Receive<GetSuccess>(HandleGetSuccess);
        Receive<NotFound>(HandleNotFound);
        Receive<GetFailure>(failure =>
            Log.Warning("Failed to read shared records: {Failure}", failure));
        Receive<Changed>(HandleChanged);
        Receive<UpsertSharedRecord>(HandleUpsert);
        Receive<DeleteSharedRecord>(HandleDelete);
        Receive<IUpdateResponse>(_ => { });
    }

    private void HandleGetSuccess(GetSuccess message)
    {
        if (Equals(message.Key, RecentMapKey))
        {
            Replace(recent, message.Get(RecentMapKey));
            TrimSeeds(pendingRecentSeeds, recent);
            CompleteInitialization();
        }
        else if (Equals(message.Key, BackupMapKey))
        {
            Replace(backups, message.Get(BackupMapKey));
            TrimSeeds(pendingBackupSeeds, backups);
            CompleteInitialization();
        }
    }

    private void HandleNotFound(NotFound message)
    {
        if (Equals(message.Key, RecentMapKey))
        {
            recent.Clear();
            CompleteInitialization();
        }
        else if (Equals(message.Key, BackupMapKey))
        {
            backups.Clear();
            CompleteInitialization();
        }
    }

    private void HandleChanged(Changed message)
    {
        if (Equals(message.Key, RecentMapKey))
        {
            Replace(recent, message.Get(RecentMapKey));
            TrimSeeds(pendingRecentSeeds, recent);
            MergePending(pendingRecentSeeds, recent);
        }
        else if (Equals(message.Key, BackupMapKey))
        {
            Replace(backups, message.Get(BackupMapKey));
            TrimSeeds(pendingBackupSeeds, backups);
            MergePending(pendingBackupSeeds, backups);
        }
        else
        {
            return;
        }

        if (initialized) PublishSnapshot();
    }

    private void HandleUpsert(UpsertSharedRecord message)
    {
        var key = message.IsBackup ? BackupMapKey : RecentMapKey;
        var clock = message.IsBackup ? BackupClock : RecentClock;
        var recordKey = message.Key;
        var value = message.Value;
        replicator
            .Ask<IUpdateResponse>(Update(
                key, LWWDictionary<string, string>.Empty, WriteAll,
                map => map.SetItem(cluster, recordKey, value, clock)))
            .ContinueWith(_ => state.NotifyUpdateCompleted(recordKey), TaskContinuationOptions.ExecuteSynchronously);
    }

    private void HandleDelete(DeleteSharedRecord message)
    {
        var key = message.IsBackup ? BackupMapKey : RecentMapKey;
        var clock = message.IsBackup ? BackupClock : RecentClock;
        var recordKey = message.Key;
        var value = message.Value;
        replicator
            .Ask<IUpdateResponse>(Update(
                key, LWWDictionary<string, string>.Empty, WriteAll,
                map => map.SetItem(cluster, recordKey, value, clock)))
            .ContinueWith(_ => state.NotifyUpdateCompleted(recordKey), TaskContinuationOptions.ExecuteSynchronously);
    }

    private void CompleteInitialization()
    {
        if (++initializationCount < 2) return;
        initialized = true;
        SeedMissing(RecentMapKey, pendingRecentSeeds, recent, RecentClock);
        SeedMissing(BackupMapKey, pendingBackupSeeds, backups, BackupClock);
        MergePending(pendingRecentSeeds, recent);
        MergePending(pendingBackupSeeds, backups);
        PublishSnapshot();
        Log.Information("Shared records initialized: {RecentCount} recent, {BackupCount} backups",
            recent.Count, backups.Count);
    }

    private void SeedMissing(
        LWWDictionaryKey<string, string> key,
        Dictionary<string, string> seeds,
        Dictionary<string, string> current,
        Clock<string> clock)
    {
        foreach (var (recordKey, recordValue) in seeds)
        {
            if (current.ContainsKey(recordKey)) continue;
            replicator
                .Ask<IUpdateResponse>(Update(
                    key, LWWDictionary<string, string>.Empty, WriteAll,
                    map => map.SetItem(cluster, recordKey, recordValue, clock)))
                .PipeTo(Self);
        }
    }

    private void PublishSnapshot()
    {
        state.ApplySnapshot(new SharedRecordsSnapshot(
            new Dictionary<string, string>(recent, StringComparer.Ordinal),
            new Dictionary<string, string>(backups, StringComparer.Ordinal)));
    }

    private static void Replace(Dictionary<string, string> target, LWWDictionary<string, string> source)
    {
        target.Clear();
        foreach (var pair in source.Entries)
            target[pair.Key] = pair.Value;
    }

    private static void TrimSeeds(Dictionary<string, string> seeds, Dictionary<string, string> current)
    {
        foreach (var key in current.Keys)
            seeds.Remove(key);
    }

    private static void MergePending(Dictionary<string, string> seeds, Dictionary<string, string> current)
    {
        foreach (var pair in seeds)
            current[pair.Key] = pair.Value;
    }
}