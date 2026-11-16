using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Serilog;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Services;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Owns the lifecycle of the local peer runtime used for shared record synchronization.
/// </summary>
internal interface IClusterRuntime
{
    void Start();

    Task StopAsync();
}

/// <summary>
///     Receiver of the local record mutations that must be replicated.
/// </summary>
internal interface ISharedRecordPublisher
{
    void Publish(SharedRecordCommand command);
}

/// <summary>
///     Message exchanged between application instances over a local named pipe.
/// </summary>
internal sealed record PeerMessage(string Type, SharedRecordCommand? Command, SnapshotDocument? Snapshot);

/// <summary>
///     Wire representation of a full record snapshot.
/// </summary>
internal sealed record SnapshotDocument(
    Dictionary<string, string> RecentFiles,
    Dictionary<string, string> Backups);

/// <summary>
///     No consensus, no leader: every instance broadcasts local mutations to the other live
///     instances and periodically pulls their snapshots. Conflicts are resolved by
///     last-writer-wins, so all instances converge on the same records.
/// </summary>
internal sealed class ClusterRuntime(
    ISharedRecordsService state,
    InstanceRegistry registry) : IClusterRuntime, ISharedRecordPublisher, IDisposable
{
    private const string CommandMessage = "command";
    private const string SyncRequest = "sync-request";
    private const string SyncResponse = "sync-response";
    private const string PipePrefix = "Witcher3StringEditor.SharedRecords";
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(10);
    private static readonly UTF8Encoding Utf8 = new(false);

    private readonly ConcurrentDictionary<string, SharedRecordCommand> pending = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim publishSignal = new(0, int.MaxValue);
    private Guid instanceId;
    private CancellationTokenSource? lifetime;
    private string pipeName = string.Empty;

    public void Start()
    {
        instanceId = Guid.NewGuid();
        pipeName = $"{PipePrefix}.{(DebugHelper.IsDebug ? "Debug" : "Release")}.{instanceId:N}";
        lifetime = new CancellationTokenSource();
        state.AttachPublisher(this);

        registry.Register(instanceId, pipeName);

        var token = lifetime.Token;
        _ = Task.Run(() => RunAsync(token), token);
    }

    public void Publish(SharedRecordCommand command)
    {
        pending[command.Key] = command;
        publishSignal.Release();
    }

    public async Task StopAsync()
    {
        try
        {
            await state.FlushAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            Log.Warning(ex, "Shared records were not fully broadcast before shutdown");
        }

        if (lifetime is { } cancellation) await cancellation.CancelAsync().ConfigureAwait(false);
        registry.Unregister(instanceId);
        lifetime?.Dispose();
        lifetime = null;
    }

    public void Dispose()
    {
        lifetime?.Dispose();
        lifetime = null;
        publishSignal.Dispose();
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll(
                PublishLoopAsync(token),
                RunPipeServerAsync(token),
                SynchronizationLoopAsync(token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The runtime is shutting down.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Shared record peer runtime stopped unexpectedly");
        }
    }

    /// <summary>
    ///     Broadcasts queued mutations. A mutation leaves the queue as soon as one live peer
    ///     accepted it; the periodic snapshot pull covers peers that were temporarily offline.
    /// </summary>
    private async Task PublishLoopAsync(CancellationToken token)
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

            var peers = GetPeerPipes();
            foreach (var (key, command) in pending.ToArray())
            {
                if (token.IsCancellationRequested) return;

                var delivered = peers.Count == 0
                                || await TrySendToAnyAsync(peers, command, token).ConfigureAwait(false);
                if (!delivered) break;

                var hasNewerValue = pending.TryGetValue(key, out var current) && current != command;
                if (hasNewerValue) continue;

                pending.TryRemove(key, out _);
                state.NotifyUpdateCompleted(key);
            }
        }
    }

    private static async Task<bool> TrySendToAnyAsync(
        IReadOnlyList<string> peers, SharedRecordCommand command, CancellationToken token)
    {
#pragma warning disable S3267 // Awaits cannot be expressed with LINQ Any.
        foreach (var peer in peers)
            if (await SendAsync(peer, CommandMessage, command, token).ConfigureAwait(false) is not null)
                return true;
#pragma warning restore S3267
        return false;
    }

    /// <summary>
    ///     Pulls the full record set from every live peer. This is the anti-entropy pass that
    ///     repairs anything missed while a peer was not running.
    /// </summary>
    private async Task SynchronizationLoopAsync(CancellationToken token)
    {
        var firstRound = true;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(firstRound ? TimeSpan.FromSeconds(2) : SyncInterval, token)
                    .ConfigureAwait(false);
                firstRound = false;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            foreach (var peer in GetPeerPipes())
            {
                if (token.IsCancellationRequested) return;

                var response = await SendAsync(peer, SyncRequest, null, token).ConfigureAwait(false);
                if (response is null) continue;

                PeerMessage? message;
                try
                {
                    message = JsonSerializer.Deserialize<PeerMessage>(response);
                }
                catch (JsonException ex)
                {
                    Log.Debug(ex, "Ignoring a malformed shared record snapshot");
                    continue;
                }

                if (message is { Type: SyncResponse, Snapshot: { } snapshot }) Merge(snapshot);
            }
        }
    }

    private void Merge(SnapshotDocument snapshot)
    {
        foreach (var pair in snapshot.RecentFiles)
            state.TryApplyRemote(new SharedRecordCommand(pair.Key, pair.Value, false));
        foreach (var pair in snapshot.Backups)
            state.TryApplyRemote(new SharedRecordCommand(pair.Key, pair.Value, true));
    }

    private IReadOnlyList<string> GetPeerPipes()
    {
        return
        [
            .. registry.ReadLive()
                .Where(x => x.InstanceId != instanceId && !string.IsNullOrEmpty(x.Address))
                .Select(x => x.Address)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }

    private async Task RunPipeServerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Byte,
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
                Log.Debug(ex, "Failed to accept a shared record connection");
                continue;
            }

            _ = Task.Run(() => HandleClientAsync(server, token), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken token)
    {
        await using (server)
        {
            try
            {
                using var reader = new StreamReader(server, Utf8, leaveOpen: true);
                await using var writer = new StreamWriter(server, Utf8, leaveOpen: true);
                writer.AutoFlush = true;

                var request = await reader.ReadLineAsync(token).ConfigureAwait(false);
                var reply = "error";
                if (request is not null)
                {
                    var message = JsonSerializer.Deserialize<PeerMessage>(request);
                    switch (message?.Type)
                    {
                        case CommandMessage when message.Command is not null:
                            state.TryApplyRemote(message.Command);
                            reply = "ok";
                            break;
                        case SyncRequest:
                            reply = JsonSerializer.Serialize(new PeerMessage(
                                SyncResponse, null, ToDocument(state.CreateCurrentSnapshot())));
                            break;
                    }
                }

                await writer.WriteLineAsync(reply.AsMemory(), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The application is shutting down.
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                Log.Debug(ex, "Failed to handle a shared record message");
            }
        }
    }

    private static SnapshotDocument ToDocument(SharedRecordsSnapshot snapshot)
    {
        return new SnapshotDocument(
            new Dictionary<string, string>(snapshot.RecentFiles, StringComparer.Ordinal),
            new Dictionary<string, string>(snapshot.Backups, StringComparer.Ordinal));
    }

    private static async Task<string?> SendAsync(
        string target, string type, SharedRecordCommand? command, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await using var client = new NamedPipeClientStream(
                ".", target, PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(timeout.Token).ConfigureAwait(false);

            await using var writer = new StreamWriter(client, Utf8, leaveOpen: true);
            writer.AutoFlush = true;
            using var reader = new StreamReader(client, Utf8, leaveOpen: true);

            var request = JsonSerializer.Serialize(new PeerMessage(type, command, null));
            await writer.WriteLineAsync(request.AsMemory(), timeout.Token).ConfigureAwait(false);
            return await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or IOException
                                       or UnauthorizedAccessException)
        {
            Log.Debug(ex, "Shared record synchronization with {Peer} failed", target);
            return null;
        }
    }
}
