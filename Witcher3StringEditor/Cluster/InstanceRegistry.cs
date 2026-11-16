using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Serilog;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Cluster;

/// <summary>
///     Metadata describing one running instance of the application.
/// </summary>
internal sealed record InstanceRegistration(
    Guid InstanceId,
    string Address,
    int ProcessId,
    DateTime UpdatedAtUtc);

/// <summary>
///     Maintains a metadata-only registry of running instances so that new processes can find
///     peers to join. No application records are ever written to this registry.
/// </summary>
internal interface IInstanceRegistry : IDisposable
{
    /// <summary>
    ///     Registers the current instance under the address peers use to reach it.
    /// </summary>
    void Register(Guid instanceId, string address);

    /// <summary>
    ///     Returns the live registrations and prunes entries whose process no longer exists.
    /// </summary>
    IReadOnlyList<InstanceRegistration> GetLiveInstances();

    /// <summary>
    ///     Removes the current instance from the registry.
    /// </summary>
    void Unregister(Guid instanceId);
}

/// <summary>
///     Maintains a metadata-only registry of running instances so that new processes can find
///     peers to join. No application records are ever written to this registry.
/// </summary>
internal sealed class InstanceRegistry : IInstanceRegistry
{
    private const string FileName = "instances.json";
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    private readonly string filePath = Path.Combine(AppPaths.ClusterRegistryDirectory, FileName);

    private readonly string mutexName = DebugHelper.IsDebug
        ? "Witcher3StringEditor_ClusterRegistry_Debug"
        : "Witcher3StringEditor_ClusterRegistry";

    private Timer? heartbeat;

    /// <summary>
    ///     Stops the heartbeat timer.
    /// </summary>
    public void Dispose()
    {
        StopHeartbeat();
    }

    /// <summary>
    ///     Registers the current instance. The address is the named pipe through which peers
    ///     exchange shared records.
    /// </summary>
    public void Register(Guid instanceId, string address)
    {
        Directory.CreateDirectory(AppPaths.ClusterRegistryDirectory);
        var registered = WithMutexLock(() =>
        {
            var registrations = Load();
            if (registrations is null) return false;

            registrations.RemoveAll(x => !IsAlive(x));
            registrations.RemoveAll(x => x.InstanceId == instanceId);
            registrations.Add(new InstanceRegistration(instanceId, address,
                Environment.ProcessId, DateTime.UtcNow));
            return Save(registrations);
        });

        if (registered) StartHeartbeat(instanceId);
    }

    /// <summary>
    ///     Returns the live registrations and prunes entries whose process no longer exists.
    /// </summary>
    public IReadOnlyList<InstanceRegistration> GetLiveInstances()
    {
        if (!File.Exists(filePath)) return [];
        IReadOnlyList<InstanceRegistration> live = [];
        WithMutexLock(() =>
        {
            var registrations = Load();
            if (registrations is null) return false;

            live = [.. registrations.Where(IsAlive)];
            return live.Count == registrations.Count || Save(live);
        });
        return live;
    }

    /// <summary>
    ///     Removes the current instance from the registry.
    /// </summary>
    public void Unregister(Guid instanceId)
    {
        StopHeartbeat();
        if (!File.Exists(filePath)) return;
        WithMutexLock(() =>
        {
            var registrations = Load();
            if (registrations is null) return false;

            return registrations.RemoveAll(x => x.InstanceId == instanceId) == 0
                   || Save(registrations);
        });
    }

    /// <summary>
    ///     Refreshes the registration while the instance runs, so another launch does not
    ///     treat a long-running instance as stale and self-join a separate cluster.
    /// </summary>
    private void StartHeartbeat(Guid instanceId)
    {
        StopHeartbeat();
        heartbeat = new Timer(_ => Heartbeat(instanceId), null, HeartbeatInterval, HeartbeatInterval);
    }

    private void StopHeartbeat()
    {
        Interlocked.Exchange(ref heartbeat, null)?.Dispose();
    }

    private void Heartbeat(Guid instanceId)
    {
        if (!File.Exists(filePath)) return;
        WithMutexLock(() =>
        {
            var registrations = Load();
            if (registrations is null) return false;

            var index = registrations.FindIndex(x => x.InstanceId == instanceId);
            if (index < 0) return true;

            registrations[index] = registrations[index] with { UpdatedAtUtc = DateTime.UtcNow };
            return Save(registrations);
        });
    }

    /// <summary>
    ///     Runs a registry mutation while holding the cross-process lock, and reports whether it ran.
    /// </summary>
    private bool WithMutexLock(Func<bool> operation)
    {
        using var mutex = new Mutex(false, mutexName);
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(3));
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            return acquired && operation();
        }
        finally
        {
            if (acquired) mutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Reads the registry, or returns null when it exists but cannot be read.
    /// </summary>
    private List<InstanceRegistration>? Load()
    {
        try
        {
            if (!File.Exists(filePath)) return [];
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<InstanceRegistration>>(json) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Failed to read the cluster instance registry");
            return null;
        }
    }

    /// <summary>
    ///     Writes the registry, or returns false when it could not be persisted.
    /// </summary>
    private bool Save(IReadOnlyCollection<InstanceRegistration> registrations)
    {
        try
        {
            var temporary = filePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(registrations));
            File.Move(temporary, filePath, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Failed to write the cluster instance registry");
            return false;
        }
    }

    private static bool IsAlive(InstanceRegistration registration)
    {
        if (DateTime.UtcNow - registration.UpdatedAtUtc > StaleAfter) return false;
        try
        {
            using var process = Process.GetProcessById(registration.ProcessId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}