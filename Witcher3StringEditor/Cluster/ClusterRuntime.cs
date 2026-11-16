using Akka.Actor;
using Akka.Configuration;
using Akka.DistributedData;
using Serilog;
using Witcher3StringEditor.Services;
using AkkaCluster = Akka.Cluster.Cluster;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Owns the lifecycle of the local Akka cluster used for shared record replication.
/// </summary>
internal interface IClusterRuntime
{
    void Start();

    Task StopAsync();
}

/// <summary>
///     Creates an ActorSystem per application instance, discovers local peers through a
///     metadata-only registry, and hosts the shared-records coordinator actor.
/// </summary>
internal sealed class ClusterRuntime(
    ISharedRecordsService state,
    InstanceRegistry registry) : IClusterRuntime
{
    private const string SystemName = "Witcher3StringEditor";

    private static readonly Config ClusterConfig = ConfigurationFactory.ParseString("""
        akka {
          actor.provider = cluster
          remote.dot-netty.tcp {
            hostname = 127.0.0.1
            port = 0
          }
          cluster {
            seed-nodes = []
            auto-down-unreachable-after = 20s
            distributed-data {
              gossip-interval = 1s
              notify-subscribers-interval = 0.25s
              durable {
                keys = []
              }
            }
          }
        }
        """);

    private Guid instanceId;
    private ActorSystem? system;

    public void Start()
    {
        instanceId = Guid.NewGuid();
        system = ActorSystem.Create(SystemName, ClusterConfig);
        var cluster = AkkaCluster.Get(system);
        var selfAddress = WaitForBoundAddress(cluster);

        var peers = registry.Register(instanceId, selfAddress.ToString());
        JoinCluster(cluster, peers);

        var replicator = DistributedData.Get(system).Replicator;
        var initialSnapshot = state.CreateCurrentSnapshot();
        var coordinator = system.ActorOf(
            Props.Create(() => new SharedRecordsCoordinator(state, replicator, initialSnapshot)),
            "shared-records");
        state.AttachCoordinator(coordinator);
        Log.Information("Cluster instance {InstanceId} started at {Address} with {PeerCount} peers",
            instanceId, selfAddress, peers.Count);
    }

    public async Task StopAsync()
    {
        if (system is null) return;
        try
        {
            await state.FlushAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            registry.Unregister(instanceId);
            var cluster = AkkaCluster.Get(system);
            cluster.Leave(cluster.SelfAddress);
            await system.Terminate().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop the cluster instance {InstanceId} gracefully", instanceId);
        }
        finally
        {
            system = null;
        }
    }

    private static Address WaitForBoundAddress(AkkaCluster cluster)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (cluster.SelfAddress.Port > 0) return cluster.SelfAddress;
            Thread.Sleep(50);
        }

        return cluster.SelfAddress;
    }

    private static void JoinCluster(AkkaCluster cluster, IReadOnlyList<InstanceRegistration> peers)
    {
        foreach (var address in peers.Select(peer => peer.Address))
            try
            {
                cluster.Join(Address.Parse(address));
                Log.Information("Joining cluster via peer {PeerAddress}", address);
                return;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                Log.Warning(ex, "Ignoring invalid peer address {PeerAddress}", address);
            }

        cluster.Join(cluster.SelfAddress);
        Log.Information("No live peer found; self-joining a new cluster at {Address}", cluster.SelfAddress);
    }
}