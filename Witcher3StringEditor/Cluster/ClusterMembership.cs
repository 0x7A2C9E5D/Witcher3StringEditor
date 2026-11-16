using System.Collections.Concurrent;
using System.IO;
using System.Net;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using Serilog;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Keeps the Raft membership in sync with the live instance registry and self-heals a node
///     left without a leader.
/// </summary>
internal sealed class ClusterMembership(
    IInstanceRegistry registry,
    Func<RaftCluster?> getNode,
    Func<Guid> getInstanceId,
    Func<bool> isLeader,
    Func<Task> restartAsStandalone)
{
    private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LeaderlessRestartDelay = TimeSpan.FromSeconds(12);
    private readonly ConcurrentDictionary<string, Guid> endpointOwners = new(StringComparer.OrdinalIgnoreCase);
    private DateTime lastLeaderSeenUtc = DateTime.UtcNow;
    private int restarting;

    /// <summary>
    ///     Records the initial peers and prunes their stale state directories.
    /// </summary>
    public void Initialize(IReadOnlyList<InstanceRegistration> peers)
    {
        RememberPeers(peers);
        CleanupStateDirectories(peers);
    }

    /// <summary>
    ///     Refreshes the endpoint-to-instance map and returns the live peers.
    /// </summary>
    public IReadOnlyList<InstanceRegistration> RefreshPeers()
    {
        var live = registry.GetLiveInstances();
        RememberPeers(live);
        return live;
    }

    /// <summary>
    ///     Gets the instance identifier that owns a Raft endpoint, if known.
    /// </summary>
    public Guid? ResolveOwner(string endpoint) =>
        endpointOwners.TryGetValue(endpoint, out var owner) ? owner : null;

    /// <summary>
    ///     Marks leader activity, or logs that consensus is unavailable.
    /// </summary>
    public void ObserveLeader(IClusterMember? leader)
    {
        lastLeaderSeenUtc = DateTime.UtcNow;
        if (leader is null)
            Log.Warning("Raft cluster cannot reach consensus; a new leader is required");
    }

    /// <summary>
    ///     Periodically synchronizes membership and attempts self-heal when leaderless.
    /// </summary>
    public async Task RunMaintenanceLoopAsync(CancellationToken token)
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
                var node = getNode();
                if (node is null) continue;

                if (node.Leader is not null) lastLeaderSeenUtc = DateTime.UtcNow;

                if (isLeader())
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

    private void RememberPeers(IReadOnlyList<InstanceRegistration> registrations)
    {
        var ownId = getInstanceId();
        foreach (var registration in registrations)
            if (registration.InstanceId != ownId)
                endpointOwners[registration.Address] = registration.InstanceId;
    }

    private async Task SynchronizeMembershipAsync(
        RaftCluster node, IReadOnlyList<InstanceRegistration> livePeers, CancellationToken token)
    {
        var ownId = getInstanceId();
        var liveAddresses = livePeers
            .Where(x => x.InstanceId != ownId)
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
        var ownId = getInstanceId();
        if (livePeers.Any(x => x.InstanceId != ownId)) return;
        if (DateTime.UtcNow - lastLeaderSeenUtc < LeaderlessRestartDelay) return;
        if (Interlocked.Exchange(ref restarting, 1) == 1) return;

        try
        {
            Log.Warning("No Raft leader and no live peers remain; restarting as a standalone node");
            await restartAsStandalone().WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The application is shutting down.
        }
        finally
        {
            Interlocked.Exchange(ref restarting, 0);
        }
    }

    private void CleanupStateDirectories(IReadOnlyList<InstanceRegistration> peers)
    {
        var root = Path.Combine(AppPaths.ClusterRegistryDirectory, "State");
        if (!Directory.Exists(root)) return;

        var live = peers.Select(x => x.InstanceId.ToString("N")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        live.Add(getInstanceId().ToString("N"));
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
