using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.StateMachine;
using Serilog;
using Witcher3StringEditor.Miscellaneous;
using Witcher3StringEditor.Services;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Owns the lifecycle of the local Raft cluster used for shared record replication.
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
///     Hosts one Raft node per application instance. The leader appends mutations to its log;
///     a follower forwards them to the leader over a local named pipe, because Raft writes are
///     leader-only. Membership is derived from the metadata-only instance registry.
/// </summary>
internal sealed class ClusterRuntime(
    ISharedRecordsService state,
    InstanceRegistry registry,
    ClusterLeadership leadership) : IClusterRuntime, ISharedRecordPublisher, IDisposable
{
    private const string PipePrefix = "Witcher3StringEditor.SharedRecords";
    private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LeaderlessRestartDelay = TimeSpan.FromSeconds(12);

    private readonly ConcurrentDictionary<string, Guid> endpointOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SharedRecordCommand> pending = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim publishSignal = new(0, int.MaxValue);
    private WriteAheadLog? auditTrail;
    private RaftCluster? cluster;
    private Guid instanceId;
    private DateTime lastLeaderSeenUtc;
    private CancellationTokenSource? lifetime;
    private IPEndPoint? localEndpoint;
    private int restarting;

    private bool IsLeader => Volatile.Read(ref cluster) is { LeadershipToken.IsCancellationRequested: false };

    public void Start()
    {
        instanceId = Guid.NewGuid();
        localEndpoint = ReserveLoopbackEndpoint();
        lifetime = new CancellationTokenSource();
        lastLeaderSeenUtc = DateTime.UtcNow;
        state.AttachPublisher(this);
        leadership.Attach(() => IsLeader);

        registry.Register(instanceId, localEndpoint.ToString());
        var peers = registry.ReadLive();
        RememberPeers(peers);
        CleanupStateDirectories(peers);

        // The first live instance bootstraps the cluster; later ones stay frozen until the
        // leader adds them, which prevents two independent single-node clusters from electing.
        var coldStart = peers.All(x => x.InstanceId == instanceId);
        var token = lifetime.Token;
        _ = Task.Run(() => RunAsync(coldStart, token), token);
    }

    public async Task StopAsync()
    {
        try
        {
            await state.FlushAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            Log.Warning(ex, "Shared records were not fully replicated before shutdown");
        }

        if (lifetime is { } cancellation) await cancellation.CancelAsync().ConfigureAwait(false);

        var node = Interlocked.Exchange(ref cluster, null);
        if (node is not null)
        {
            node.LeaderChanged -= OnLeaderChanged;
            try
            {
                await node.StopAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to stop the Raft node gracefully");
            }
        }

        var wal = Interlocked.Exchange(ref auditTrail, null);
        if (wal is not null)
            try
            {
                await wal.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to dispose the Raft write-ahead log");
            }

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

    public void Publish(SharedRecordCommand command)
    {
        pending[command.Key] = command;
        publishSignal.Release();
    }

    public void PublishAll(SharedRecordsSnapshot snapshot)
    {
        foreach (var pair in snapshot.RecentFiles)
            pending[pair.Key] = new SharedRecordCommand(pair.Key, pair.Value, false);
        foreach (var pair in snapshot.Backups)
            pending[pair.Key] = new SharedRecordCommand(pair.Key, pair.Value, true);
        publishSignal.Release();
    }

    private static IPEndPoint ReserveLoopbackEndpoint()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new IPEndPoint(IPAddress.Loopback, port);
    }

    private static string PipeName(Guid id)
    {
        return $"{PipePrefix}.{(DebugHelper.IsDebug ? "Debug" : "Release")}.{id:N}";
    }

    private async Task RunAsync(bool coldStart, CancellationToken token)
    {
        try
        {
            await StartCoreAsync(coldStart, token).ConfigureAwait(false);
            await Task.WhenAll(
                PublishLoopAsync(token),
                RunPipeServerAsync(token),
                MaintenanceLoopAsync(token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The runtime is shutting down.
        }
        catch (ObjectDisposedException)
        {
            // The runtime has already been disposed.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Raft cluster pipeline stopped unexpectedly");
        }
    }

    private async Task StartCoreAsync(bool coldStart, CancellationToken token)
    {
        var stateDirectory = new DirectoryInfo(Path.Combine(
            AppPaths.ClusterRegistryDirectory, "State", instanceId.ToString("N")));
        Directory.CreateDirectory(stateDirectory.FullName);

        // Snapshots live in their own folder so they never collide with the log segments.
        var snapshotDirectory = stateDirectory.CreateSubdirectory("Snapshots");
        var stateMachine = new SharedRecordsStateMachine(snapshotDirectory);
        await stateMachine.RestoreAsync(token).ConfigureAwait(false);

        var wal = new WriteAheadLog(
            new WriteAheadLog.Options { Location = stateDirectory.FullName },
            stateMachine);

        var configuration = new RaftCluster.TcpConfiguration(localEndpoint!)
        {
            ColdStart = coldStart,
            ConfigurationStorage = null, // in-memory membership, rebuilt from the instance registry
            LowerElectionTimeout = 500,
            UpperElectionTimeout = 1200,
            RequestTimeout = TimeSpan.FromSeconds(5),
            GracefulShutdownTimeout = TimeSpan.FromSeconds(2)
        };

        var node = new RaftCluster(configuration) { AuditTrail = wal };
        node.LeaderChanged += OnLeaderChanged;
        stateMachine.SnapshotProduced += OnSnapshotProduced;

        await node.StartAsync(token).ConfigureAwait(false);

        auditTrail = wal;
        cluster = node;
        stateMachine.PublishCurrentState();
        PublishAll(state.CreateCurrentSnapshot());
        registry.Register(instanceId, node.LocalMemberAddress.ToString() ?? string.Empty);

        Log.Information(
            "Raft node {InstanceId} started at {@Address} (cold start: {ColdStart})",
            instanceId, node.LocalMemberAddress, coldStart);
    }

    private void OnLeaderChanged(ICluster sender, IClusterMember? leader)
    {
        lastLeaderSeenUtc = DateTime.UtcNow;
        if (leader is null)
        {
            Log.Warning("Raft cluster cannot reach consensus; a new leader is required");
            return;
        }

        Log.Information("Raft leader is {@Address} (remote: {IsRemote})", leader.EndPoint, leader.IsRemote);
        if (!leader.IsRemote || lifetime is not { IsCancellationRequested: false } cancellation) return;

        // Re-publish the local records after joining a leader: anything written while this node
        // was still standalone must reach the replicated state. Last-writer-wins rejects stale values.
        var token = cancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                if (!token.IsCancellationRequested) PublishAll(state.CreateCurrentSnapshot());
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                // The application is shutting down.
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to re-publish local records after the leader changed");
            }
        }, token);
    }

    private void OnSnapshotProduced(SharedRecordsSnapshot snapshot)
    {
        try
        {
            state.ApplySnapshot(snapshot);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply the replicated shared records to the UI");
        }
    }

    /// <summary>
    ///     Drains queued mutations. The leader appends them to its log; a follower forwards
    ///     them to the leader. Failed mutations stay queued and are retried on the next tick.
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

            if (Volatile.Read(ref cluster) is null) continue;

            foreach (var (key, command) in pending.ToArray())
            {
                if (token.IsCancellationRequested) return;
                if (!await TryPublishAsync(command, token).ConfigureAwait(false)) break;

                var hasNewerValue = pending.TryGetValue(key, out var current) && current != command;
                if (hasNewerValue) continue;

                pending.TryRemove(key, out _);
                state.NotifyUpdateCompleted(key);
            }
        }
    }

    private async Task<bool> TryPublishAsync(SharedRecordCommand command, CancellationToken token)
    {
        var node = Volatile.Read(ref cluster);
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
        if (endpointOwners.TryGetValue(address, out var ownerId))
            return await SendToPipeAsync(ownerId, payload, token).ConfigureAwait(false);
        RefreshPeers();
        if (!endpointOwners.TryGetValue(address, out ownerId)) return false;

        return await SendToPipeAsync(ownerId, payload, token).ConfigureAwait(false);
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

    private async Task RunPipeServerAsync(CancellationToken token)
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
                var node = Volatile.Read(ref cluster);
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

    private async Task MaintenanceLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(MaintenanceInterval, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                var livePeers = RefreshPeers();
                var node = Volatile.Read(ref cluster);
                if (node is null) continue;

                if (node.Leader is not null) lastLeaderSeenUtc = DateTime.UtcNow;

                if (!node.LeadershipToken.IsCancellationRequested)
                    await SynchronizeMembershipAsync(node, livePeers, token).ConfigureAwait(false);
                else
                    await TrySelfHealAsync(livePeers, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The application is shutting down.
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to maintain the Raft cluster membership");
            }
        }
    }

    /// <summary>
    ///     Refreshes the endpoint-to-instance map and returns the live peers.
    /// </summary>
    private IReadOnlyList<InstanceRegistration> RefreshPeers()
    {
        var live = registry.ReadLive();
        RememberPeers(live);
        return live;
    }

    private void RememberPeers(IReadOnlyList<InstanceRegistration> registrations)
    {
        foreach (var registration in registrations)
            if (registration.InstanceId != instanceId)
                endpointOwners[registration.Address] = registration.InstanceId;
    }

    private async Task SynchronizeMembershipAsync(
        RaftCluster node, IReadOnlyList<InstanceRegistration> livePeers, CancellationToken token)
    {
        var liveAddresses = livePeers
            .Where(x => x.InstanceId != instanceId)
            .Select(x => x.Address)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var memberAddresses = node.Members
            .Where(x => x.IsRemote)
            .Select(x => x.EndPoint.ToString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var address in liveAddresses.Where(x => !memberAddresses.Contains(x)))
            {
                if (!IPEndPoint.TryParse(address, out var endpoint)) continue;
                Log.Information("Adding Raft member {Address}", address);
                await node.AddMemberAsync(endpoint, token).ConfigureAwait(false);
            }

            foreach (var endpoint in node.Members
                         .Where(x => x.IsRemote && !liveAddresses.Contains(x.EndPoint.ToString() ?? string.Empty))
                         .Select(x => x.EndPoint))
            {
                Log.Information("Removing stale Raft member {@Address}", endpoint);
                await node.RemoveMemberAsync(endpoint, token).ConfigureAwait(false);
            }
        }
        catch (NotLeaderException)
        {
            // Leadership changed while the membership was being modified; the next tick retries.
        }
        catch (OperationCanceledException)
        {
            // The application is shutting down.
        }
    }

    /// <summary>
    ///     A follower cannot remove a dead leader from the membership, so a node left alone in a
    ///     two-node cluster restarts as a standalone cluster instead of blocking all writes.
    /// </summary>
    private async Task TrySelfHealAsync(IReadOnlyList<InstanceRegistration> livePeers, CancellationToken token)
    {
        if (livePeers.Any(x => x.InstanceId != instanceId)) return;
        if (DateTime.UtcNow - lastLeaderSeenUtc < LeaderlessRestartDelay) return;
        if (Interlocked.Exchange(ref restarting, 1) == 1) return;

        try
        {
            Log.Warning("No Raft leader and no live peers remain; restarting as a standalone node");
            await RestartAsStandaloneAsync(token).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref restarting, 0);
        }
    }

    private async Task RestartAsStandaloneAsync(CancellationToken token)
    {
        var node = Interlocked.Exchange(ref cluster, null);
        if (node is not null)
        {
            node.LeaderChanged -= OnLeaderChanged;
            try
            {
                await node.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to stop the Raft node before restarting it");
            }
        }

        var wal = Interlocked.Exchange(ref auditTrail, null);
        if (wal is not null)
            try
            {
                await wal.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to dispose the Raft write-ahead log before restarting");
            }

        lastLeaderSeenUtc = DateTime.UtcNow;
        await StartCoreAsync(true, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes state directories of instances that are no longer registered.
    /// </summary>
    private void CleanupStateDirectories(IReadOnlyList<InstanceRegistration> peers)
    {
        var root = Path.Combine(AppPaths.ClusterRegistryDirectory, "State");
        if (!Directory.Exists(root)) return;

        var live = peers.Select(x => x.InstanceId.ToString("N")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        live.Add(instanceId.ToString("N"));
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (live.Contains(name)) continue;
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Debug(ex, "Failed to remove the stale Raft state directory {Directory}", directory);
            }
        }
    }
}