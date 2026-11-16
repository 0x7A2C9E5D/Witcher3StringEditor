using System.Buffers;
using System.IO;
using System.Text.Json;
using DotNext.IO;
using DotNext.Net.Cluster.Consensus.Raft.StateMachine;
using Serilog;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Raft state machine that materializes replicated record mutations into two maps and
///     persists them as a snapshot. Every mutation is resolved with last-writer-wins, so the
///     state converges even when the same record is changed on different instances.
/// </summary>
internal sealed class SharedRecordsStateMachine(DirectoryInfo location) : SimpleStateMachine(location)
{
    private const int SnapshotInterval = 256;
    private readonly Dictionary<string, string> backups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> recent = new(StringComparer.Ordinal);
    private readonly Lock sync = new();

    /// <summary>
    ///     Raised after the state machine changed or was restored. Runs on the log apply thread.
    /// </summary>
    public event Action<SharedRecordsSnapshot>? SnapshotProduced;

    private SharedRecordsSnapshot CreateSnapshot()
    {
        lock (sync)
        {
            return new SharedRecordsSnapshot(
                new Dictionary<string, string>(recent, StringComparer.Ordinal),
                new Dictionary<string, string>(backups, StringComparer.Ordinal));
        }
    }

    /// <summary>
    ///     Publishes the current state to the UI after the log has been replayed.
    /// </summary>
    public void PublishCurrentState()
    {
        RaiseSnapshotProduced(CreateSnapshot());
    }

    protected override ValueTask RestoreAsync(FileInfo snapshotFile, CancellationToken token)
    {
        try
        {
            var document = JsonSerializer.Deserialize<SnapshotDocument>(
                File.ReadAllBytes(snapshotFile.FullName));
            if (document is not null)
                lock (sync)
                {
                    recent.Clear();
                    backups.Clear();
                    foreach (var pair in document.RecentFiles) recent[pair.Key] = pair.Value;
                    foreach (var pair in document.Backups) backups[pair.Key] = pair.Value;
                }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Failed to restore the shared records snapshot");
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask PersistAsync(IAsyncBinaryWriter writer, CancellationToken token)
    {
        SnapshotDocument document;
        lock (sync)
        {
            document = new SnapshotDocument(
                new Dictionary<string, string>(recent, StringComparer.Ordinal),
                new Dictionary<string, string>(backups, StringComparer.Ordinal));
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(document);
        return writer.WriteAsync(payload, null, token);
    }

    protected override ValueTask<bool> ApplyAsync(LogEntry entry, CancellationToken token)
    {
        // Raft appends an empty entry when a new leader takes over and configuration entries
        // carry no record payload, so both must be ignored instead of failing the apply loop.
        if (entry.IsConfiguration || !entry.TryGetPayload(out var payload) || payload.IsEmpty)
            return new ValueTask<bool>(false);

        SharedRecordCommand? command;
        try
        {
            command = JsonSerializer.Deserialize<SharedRecordCommand>(payload.ToArray());
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Ignoring an unreadable replicated shared record at index {Index}", entry.Index);
            return new ValueTask<bool>(false);
        }

        if (command is null)
            return new ValueTask<bool>(false);

        bool changed;
        lock (sync)
        {
            var map = command.IsBackup ? backups : recent;
            changed = ApplyTo(map, command);
        }

        if (changed) RaiseSnapshotProduced(CreateSnapshot());

        // Ask for a snapshot every N entries so a late joiner can catch up without a full replay.
        return new ValueTask<bool>(entry.Index % SnapshotInterval == 0L);
    }

    private static bool ApplyTo(Dictionary<string, string> map, SharedRecordCommand command)
    {
        if (!map.TryGetValue(command.Key, out var current))
        {
            map[command.Key] = command.Value;
            return true;
        }

        var incoming = SharedRecordCodec.GetVersion(command.IsBackup, command.Value);
        var existing = SharedRecordCodec.GetVersion(command.IsBackup, current);
        if (!incoming.Supersedes(existing)) return false;

        map[command.Key] = command.Value;
        return true;
    }

    private void RaiseSnapshotProduced(SharedRecordsSnapshot snapshot)
    {
        try
        {
            SnapshotProduced?.Invoke(snapshot);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to publish the replicated shared records");
        }
    }

    private sealed record SnapshotDocument(
        Dictionary<string, string> RecentFiles,
        Dictionary<string, string> Backups);
}