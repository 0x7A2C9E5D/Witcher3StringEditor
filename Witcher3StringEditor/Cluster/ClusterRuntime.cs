using System.Net;
using System.Net.Sockets;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using Serilog;
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
///     Composes the Raft node host, record replicator and membership manager into one runtime.
///     Orchestration only: each concern lives in its own component.
/// </summary>
internal sealed class ClusterRuntime(
    ISharedRecordsService state,
    IInstanceRegistry registry,
    IClusterLeadership leadership) : IClusterRuntime, ISharedRecordPublisher, IDisposable
{
    private readonly RaftNodeHost host = new();
    private RecordReplicator? replicator;
    private ClusterMembership? membership;
    private CancellationTokenSource? lifetime;
    private Guid instanceId;
    private IPEndPoint? localEndpoint;

    public void Start()
    {
        instanceId = Guid.NewGuid();
        localEndpoint = ReserveLoopbackEndpoint();
        lifetime = new CancellationTokenSource();

        replicator = new RecordReplicator(
            () => host.Node,
            address => membership?.ResolveOwner(address),
            () => { membership?.RefreshPeers(); });
        replicator.Attach(instanceId);

        membership = new ClusterMembership(
            registry,
            () => host.Node,
            () => instanceId,
            () => host.IsLeader,
            RestartAsStandaloneAsync);

        state.AttachPublisher(this);
        leadership.Attach(() => host.IsLeader);
        host.LeaderChanged += OnLeaderChanged;
        host.SnapshotProduced += OnSnapshotProduced;

        registry.Register(instanceId, localEndpoint.ToString());
        var peers = registry.GetLiveInstances();
        membership.Initialize(peers);

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

        host.LeaderChanged -= OnLeaderChanged;
        host.SnapshotProduced -= OnSnapshotProduced;
        await host.StopAsync().ConfigureAwait(false);

        registry.Unregister(instanceId);
        lifetime?.Dispose();
        lifetime = null;
    }

    public void Dispose()
    {
        lifetime?.Dispose();
        lifetime = null;
        replicator?.Dispose();
        replicator = null;
    }

    /// <summary>
    ///     Queues a local record mutation for replication.
    /// </summary>
    public void Publish(SharedRecordCommand command) => replicator?.Publish(command);

    private async Task RunAsync(bool coldStart, CancellationToken token)
    {
        try
        {
            await StartCoreAsync(coldStart, token).ConfigureAwait(false);
            await Task.WhenAll(
                replicator!.RunPublishLoopAsync(state.NotifyUpdateCompleted, token),
                replicator.RunPipeServerAsync(token),
                membership!.RunMaintenanceLoopAsync(token)).ConfigureAwait(false);
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
        await host.StartAsync(instanceId, localEndpoint!, coldStart, token).ConfigureAwait(false);
        replicator!.PublishAll(state.CreateCurrentSnapshot());
        registry.Register(instanceId, host.GetLocalAddress());

        Log.Information(
            "Raft node {InstanceId} started at {Address} (cold start: {ColdStart})",
            instanceId, host.GetLocalAddress(), coldStart);
    }

    private void OnLeaderChanged(ICluster sender, IClusterMember? leader)
    {
        membership?.ObserveLeader(leader);
        if (leader is null || !leader.IsRemote || lifetime is not { IsCancellationRequested: false } cancellation)
            return;

        // Re-publish local records after joining a leader: anything written while this node
        // was still standalone must reach the replicated state. Last-writer-wins rejects stale values.
        var token = cancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                    replicator?.PublishAll(state.CreateCurrentSnapshot());
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

    private async Task RestartAsStandaloneAsync()
    {
        await host.StopAsync().ConfigureAwait(false);
        if (lifetime is not { IsCancellationRequested: false } cancellation) return;

        await host.StartAsync(instanceId, localEndpoint!, true, cancellation.Token).ConfigureAwait(false);
        replicator?.PublishAll(state.CreateCurrentSnapshot());
        registry.Register(instanceId, host.GetLocalAddress());
    }

    private static IPEndPoint ReserveLoopbackEndpoint()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new IPEndPoint(IPAddress.Loopback, port);
    }
}
