using Serilog.Events;
using Witcher3StringEditor.Contracts;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides access to the application logs
/// </summary>
/// <remarks>
///     Log events arrive from whichever thread writes them while the log dialog may read them, so the storage is
///     guarded by a lock and only ever handed out as a copy. The history is capped, because the collection lives for
///     the whole run and an unbounded log view would grow without limit
/// </remarks>
public sealed class LogAccessService : ILogAccessService
{
    /// <summary>
    ///     The maximum number of log events kept for display; older events are dropped first
    /// </summary>
    private const int MaxLogCount = 5000;

    /// <summary>
    ///     The lock guarding both the stored events and the subscribed handlers
    /// </summary>
    private readonly Lock gate = new();

    /// <summary>
    ///     The handlers invoked for every new log event
    /// </summary>
    private readonly List<Action<LogEvent>> handlers = [];

    /// <summary>
    ///     The stored log events, oldest first
    /// </summary>
    private readonly List<LogEvent> logs = new(MaxLogCount);

    /// <inheritdoc />
    public void Add(LogEvent logEvent)
    {
        lock (gate)
        {
            logs.Add(logEvent);
            if (logs.Count > MaxLogCount)
                logs.RemoveRange(0, logs.Count - MaxLogCount);

            // Invoked inside the lock so a subscriber cannot miss an event between reading the history and
            // registering, which is why handlers must not call back into this service synchronously
            foreach (var handler in handlers)
                handler(logEvent);
        }
    }

    /// <inheritdoc />
    public LogEvent[] Subscribe(Action<LogEvent> handler)
    {
        lock (gate)
        {
            handlers.Add(handler);
            return [.. logs];
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(Action<LogEvent> handler)
    {
        lock (gate)
        {
            handlers.Remove(handler);
        }
    }
}