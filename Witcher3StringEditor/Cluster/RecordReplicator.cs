using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DotNext.Net.Cluster.Consensus.Raft;
using Serilog;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Queues local record mutations and replicates them. The Raft leader appends to its own log;
///     a follower forwards mutations to the leader over a local named pipe because Raft writes
///     are leader-only.
/// </summary>
internal sealed class RecordReplicator(
    Func<RaftCluster?> getNode,
    Func<string, Guid?> resolveEndpointOwner,
    Action refreshPeers)
    : IDisposable
{
    private const string PipePrefix = "Witcher3StringEditor.SharedRecords";
    private readonly ConcurrentDictionary<string, SharedRecordCommand> pending = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim publishSignal = new(0, int.MaxValue);
    private Guid instanceId = Guid.Empty;

    public void Dispose()
    {
        publishSignal.Dispose();
    }

    /// <summary>
    ///     Sets the instance identifier used to derive the named pipe endpoint.
    /// </summary>
    public void Attach(Guid id)
    {
        instanceId = id;
    }

    /// <summary>
    ///     Queues a single mutation for replication.
    /// </summary>
    public void Publish(SharedRecordCommand command)
    {
        pending[command.Key] = command;
        publishSignal.Release();
    }

    /// <summary>
    ///     Queues a full snapshot, replacing stale pending entries with current values.
    /// </summary>
    public void PublishAll(SharedRecordsSnapshot snapshot)
    {
        foreach (var pair in snapshot.RecentFiles)
            pending[pair.Key] = new SharedRecordCommand(pair.Key, pair.Value, false);
        foreach (var pair in snapshot.Backups)
            pending[pair.Key] = new SharedRecordCommand(pair.Key, pair.Value, true);
        publishSignal.Release();
    }

    /// <summary>
    ///     Drains queued mutations until canceled. Failed writes stay queued for the next tick.
    /// </summary>
    public async Task RunPublishLoopAsync(Action<string> onCommitted, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await publishSignal.WaitAsync(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }

            if (getNode() is null) continue;

            foreach (var (key, command) in pending.ToArray())
            {
                if (token.IsCancellationRequested) return;
                if (!await TryPublishAsync(command, token).ConfigureAwait(false)) break;

                // A newer value was queued while this write was in flight; keep it pending.
                if (pending.TryGetValue(key, out var current) && current != command) continue;

                pending.TryRemove(key, out _);
                onCommitted(key);
            }
        }
    }

    /// <summary>
    ///     Serves mutations forwarded by follower instances to this (leader) node.
    /// </summary>
    public async Task RunPipeServerAsync(CancellationToken token)
    {
        var name = PipeName(instanceId);
        while (!token.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(
                name, PipeDirection.InOut, 4, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            try
            {
                await server.WaitForConnectionAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                Log.Debug(ex, "Failed to accept a shared record forwarder connection");
                continue;
            }

            _ = Task.Run(() => HandleForwarderAsync(server, token), CancellationToken.None);
        }
    }

    private async Task<bool> TryPublishAsync(SharedRecordCommand command, CancellationToken token)
    {
        var node = getNode();
        if (node is null) return false;

        var payload = JsonSerializer.SerializeToUtf8Bytes(command);
        try
        {
            if (node.LeadershipToken.IsCancellationRequested)
                return await ForwardToLeaderAsync(node, payload, token).ConfigureAwait(false);
            await node.ReplicateAsync(payload, token: token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Log.Debug(ex, "Local Raft node is not the leader; retrying the shared record write");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to replicate the shared record {Key}", command.Key);
            return false;
        }
    }

    private async Task<bool> ForwardToLeaderAsync(
        IRaftCluster node, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var leader = node.Leader;
        if (leader is null || !leader.IsRemote) return false;

        var address = leader.EndPoint.ToString() ?? string.Empty;
        var ownerId = resolveEndpointOwner(address);
        // ReSharper disable once InvertIf
        if (ownerId is null)
        {
            refreshPeers();
            ownerId = resolveEndpointOwner(address);
        }
        return ownerId is { } id && await SendToPipeAsync(id, payload, token).ConfigureAwait(false);
    }

    private static async Task<bool> SendToPipeAsync(
        Guid target, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await using var client = new NamedPipeClientStream(
                ".", PipeName(target), PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(timeout.Token).ConfigureAwait(false);

            await using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true);
            writer.AutoFlush = true;
            using var reader = new StreamReader(client, new UTF8Encoding(false), leaveOpen: true);

            await writer.WriteLineAsync(
                Encoding.UTF8.GetString(payload.Span).AsMemory(), timeout.Token).ConfigureAwait(false);
            var response = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
            return string.Equals(response, "1", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or IOException
                                       or UnauthorizedAccessException)
        {
            Log.Debug(ex, "Shared record forwarding to {TargetInstance} failed", target);
            return false;
        }
    }

    private async Task HandleForwarderAsync(NamedPipeServerStream server, CancellationToken token)
    {
        await using (server)
        {
            try
            {
                using var reader = new StreamReader(server, new UTF8Encoding(false), leaveOpen: true);
                await using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true);
                writer.AutoFlush = true;

                var request = await reader.ReadLineAsync(token).ConfigureAwait(false);
                var command = string.IsNullOrEmpty(request)
                    ? null
                    : JsonSerializer.Deserialize<SharedRecordCommand>(request);
                var node = getNode();
                var committed = false;
                if (command is not null && node is not null
                                        && !node.LeadershipToken.IsCancellationRequested)
                {
                    await node.ReplicateAsync(JsonSerializer.SerializeToUtf8Bytes(command), token: token)
                        .ConfigureAwait(false);
                    committed = true;
                }

                await writer.WriteLineAsync(committed ? "1".AsMemory() : "0".AsMemory(), token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The forwarding connection was closed while the application is shutting down.
            }
            catch (JsonException ex)
            {
                Log.Debug(ex, "Failed to handle a forwarded shared record write");
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                Log.Debug(ex, "Failed to handle a forwarded shared record write");
            }
        }
    }

    private static string PipeName(Guid id)
    {
        return $"{PipePrefix}.{(DebugHelper.IsDebug ? "Debug" : "Release")}.{id:N}";
    }
}