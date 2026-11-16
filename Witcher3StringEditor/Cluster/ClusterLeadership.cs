namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Process-wide leadership state of the local Raft node. Services that own resources shared
///     by all instances, such as the settings file, ask this class before writing.
/// </summary>
internal sealed class ClusterLeadership
{
    private volatile Func<bool>? probe;

    /// <summary>
    ///     Gets a value indicating whether this instance currently owns the shared resources.
    ///     Without a running cluster the instance is on its own and remains the owner.
    /// </summary>
    public bool IsLeader => probe?.Invoke() ?? true;

    /// <summary>
    ///     Registers the probe used to ask the cluster runtime for the current role.
    /// </summary>
    /// <param name="isLeader">Returns true when this instance is the Raft leader</param>
    public void Attach(Func<bool> isLeader)
    {
        probe = isLeader;
    }
}
