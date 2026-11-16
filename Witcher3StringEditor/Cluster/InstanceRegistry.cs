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
internal sealed class InstanceRegistry : IDisposable
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
        WithRegistryLock(() =>
        {
            var registrations = Load();
            registrations.RemoveAll(x => !IsAlive(x));
            registrations.RemoveAll(x => x.InstanceId == instanceId);
            registrations.Add(new InstanceRegistration(instanceId, address,
                Environment.ProcessId, DateTime.UtcNow));
            Save(registrations);
        });
        StartHeartbeat(instanceId);
    }

    /// <summary>
    ///     Returns the live registrations and prunes entries whose process no longer exists.
    /// </summary>
    public IReadOnlyList<InstanceRegistration> ReadLive()
    {
        if (!File.Exists(filePath)) return [];
        IReadOnlyList<InstanceRegistration> live = [];
        WithRegistryLock(() =>
        {
            var registrations = Load();
            live = [.. registrations.Where(IsAlive)];
            if (live.Count != registrations.Count) Save(live);
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
        WithRegistryLock(() =>
        {
            var registrations = Load();
            if (registrations.RemoveAll(x => x.InstanceId == instanceId) > 0)
                Save(registrations);
        });
    }

    /// <summary>
    ///     Refreshes the registration while the instance runs, so another launch does not
    ///     treat a long-running instance as stale and self-join a separate cluster.
    /// </summary>
    private void StartHeartbeat(Guid instanceId)
    {
        StopHeartbeat();
        heartbeat = new Timer(_ => Touch(instanceId), null, HeartbeatInterval, HeartbeatInterval);
    }

    private void StopHeartbeat()
    {
        Interlocked.Exchange(ref heartbeat, null)?.Dispose();
    }

    private void Touch(Guid instanceId)
    {
        if (!File.Exists(filePath)) return;
        WithRegistryLock(() =>
        {
            var registrations = Load();
            var index = registrations.FindIndex(x => x.InstanceId == instanceId);
            if (index < 0) return;
            registrations[index] = registrations[index] with { UpdatedAtUtc = DateTime.UtcNow };
            Save(registrations);
        });
    }

    private void WithRegistryLock(Action action)
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

            if (acquired) action();
        }
        finally
        {
            if (acquired) mutex.ReleaseMutex();
        }
    }

    private List<InstanceRegistration> Load()
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
            return [];
        }
    }

    private void Save(IReadOnlyCollection<InstanceRegistration> registrations)
    {
        try
        {
            var temporary = filePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(registrations));
            File.Move(temporary, filePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Failed to write the cluster instance registry");
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