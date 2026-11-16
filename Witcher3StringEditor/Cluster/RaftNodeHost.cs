using System.IO;
using System.Net;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.StateMachine;
using Serilog;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Creates and owns the Raft node and its write-ahead log for one application instance.
/// </summary>
internal sealed class RaftNodeHost
{
    private WriteAheadLog? auditTrail;
    private RaftCluster? node;
    private SharedRecordsStateMachine? stateMachine;

    /// <summary>
    ///     Gets the current Raft node, if it has been started.
    /// </summary>
    public RaftCluster? Node => Volatile.Read(ref node);

    /// <summary>
    ///     Gets a value indicating whether this node currently owns leadership.
    /// </summary>
    public bool IsLeader => Node is { LeadershipToken.IsCancellationRequested: false };

    /// <summary>
    ///     Occurs when the Raft leader changes.
    /// </summary>
    public event Action<ICluster, IClusterMember?>? LeaderChanged;

    /// <summary>
    ///     Occurs when the state machine produces a replicated snapshot.
    /// </summary>
    public event Action<SharedRecordsSnapshot>? SnapshotProduced;

    /// <summary>
    ///     Creates and starts a node using state persisted under the instance directory.
    /// </summary>
    public async Task StartAsync(
        Guid instanceId,
        IPEndPoint localEndpoint,
        bool coldStart,
        CancellationToken token)
    {
        var stateDirectory = new DirectoryInfo(Path.Combine(
            AppPaths.ClusterRegistryDirectory, "State", instanceId.ToString("N")));
        Directory.CreateDirectory(stateDirectory.FullName);

        // Snapshots live in their own folder so they never collide with the log segments.
        var machine = new SharedRecordsStateMachine(stateDirectory.CreateSubdirectory("Snapshots"));
        await machine.RestoreAsync(token).ConfigureAwait(false);
        machine.SnapshotProduced += OnSnapshotProduced;
        stateMachine = machine;

        var wal = new WriteAheadLog(
            new WriteAheadLog.Options { Location = stateDirectory.FullName }, machine);
        var configuration = new RaftCluster.TcpConfiguration(localEndpoint)
        {
            ColdStart = coldStart,
            ConfigurationStorage = null, // in-memory membership, rebuilt from the instance registry
            LowerElectionTimeout = 500,
            UpperElectionTimeout = 1200,
            RequestTimeout = TimeSpan.FromSeconds(5),
            GracefulShutdownTimeout = TimeSpan.FromSeconds(2)
        };

        var raftNode = new RaftCluster(configuration) { AuditTrail = wal };
        raftNode.LeaderChanged += OnLeaderChanged;

        await raftNode.StartAsync(token).ConfigureAwait(false);

        auditTrail = wal;
        Volatile.Write(ref node, raftNode);
        machine.PublishCurrentState();
    }

    /// <summary>
    ///     Stops the node and disposes its write-ahead log.
    /// </summary>
    public async Task StopAsync()
    {
        var raftNode = Interlocked.Exchange(ref node, null);
        if (raftNode is not null)
        {
            raftNode.LeaderChanged -= OnLeaderChanged;
            try
            {
                await raftNode.StopAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to stop the Raft node gracefully");
            }
        }

        if (stateMachine is not null)
            stateMachine.SnapshotProduced -= OnSnapshotProduced;

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

        stateMachine = null;
    }

    /// <summary>
    ///     Gets the endpoint other instances use to reach this node.
    /// </summary>
    public string GetLocalAddress()
    {
        return Node?.LocalMemberAddress.ToString() ?? string.Empty;
    }

    private void OnLeaderChanged(ICluster sender, IClusterMember? leader)
    {
        LeaderChanged?.Invoke(sender, leader);
    }

    private void OnSnapshotProduced(SharedRecordsSnapshot snapshot)
    {
        SnapshotProduced?.Invoke(snapshot);
    }
}