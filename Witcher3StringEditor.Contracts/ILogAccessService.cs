using Serilog.Events;

namespace Witcher3StringEditor.Contracts;

/// <summary>
///     Defines a contract for logging access service operations
/// </summary>
/// <remarks>
///     Log events are produced on whichever thread writes them, so the service owns its own synchronization:
///     callers never touch the underlying storage, and subscribers receive events from the thread that logs them
/// </remarks>
public interface ILogAccessService
{
    /// <summary>
    ///     Adds a log event, keeping only the most recent ones
    /// </summary>
    /// <param name="logEvent">The log event to add</param>
    void Add(LogEvent logEvent);

    /// <summary>
    ///     Registers a handler for new log events and returns the events logged before the subscription
    /// </summary>
    /// <remarks>
    ///     The history and the registration are taken atomically: an event is either in the returned history or
    ///     passed to the handler, never both and never neither. Handlers are invoked under the service lock, so a
    ///     handler must not call back into this service synchronously
    /// </remarks>
    /// <param name="handler">The handler to invoke for every new log event</param>
    /// <returns>The log events logged before the handler was registered</returns>
    LogEvent[] Subscribe(Action<LogEvent> handler);

    /// <summary>
    ///     Unregisters a handler registered by <see cref="Subscribe" />
    /// </summary>
    /// <param name="handler">The handler to unregister</param>
    void Unsubscribe(Action<LogEvent> handler);
}