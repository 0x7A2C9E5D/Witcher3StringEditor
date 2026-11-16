namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Process-wide leadership state of the local Raft node. Services that own resources shared
///     by all instances, such as the settings file, ask this contract before writing.
/// </summary>
internal interface IClusterLeadership
{
    /// <summary>
    ///     Gets a value indicating whether this instance currently owns the shared resources.
    ///     Without a running cluster the instance is on its own and remains the owner.
    /// </summary>
    bool IsLeader { get; }

    /// <summary>
    ///     Registers the resolver that reports the role of the cluster runtime on demand.
    /// </summary>
    /// <param name="isLeader">Returns true when this instance is the Raft leader</param>
    void Attach(Func<bool> isLeader);
}

/// <summary>
///     Process-wide leadership state of the local Raft node.
/// </summary>
internal sealed class ClusterLeadership : IClusterLeadership
{
    private volatile Func<bool>? leaderResolver;

    public bool IsLeader => leaderResolver?.Invoke() ?? true;

    public void Attach(Func<bool> isLeader)
    {
        leaderResolver = isLeader;
    }
}
