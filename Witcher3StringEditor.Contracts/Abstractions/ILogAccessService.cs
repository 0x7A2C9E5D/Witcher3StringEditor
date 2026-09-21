using System.Collections.ObjectModel;
using Serilog.Events;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for logging access service operations
/// </summary>
/// <remarks>
///     The service owns the log collection. Consumers only receive a read-only view and must use
///     <see cref="Add" /> / <see cref="Remove" /> for mutation, so retention and thread-affinity rules
///     cannot be bypassed. Implementations marshal mutations onto the UI thread.
/// </remarks>
public interface ILogAccessService
{
    /// <summary>
    ///     Gets a read-only view over the collected log events
    ///     Implementations cap the in-memory collection (oldest entries are evicted first) so a long-running
    ///     session cannot grow without bound; trimming is the implementer's responsibility
    /// </summary>
    ReadOnlyObservableCollection<LogEvent> Logs { get; }

    /// <summary>
    ///     Adds a log event to the collection
    /// </summary>
    /// <param name="logEvent">The log event to append</param>
    /// <remarks>
    ///     Safe to call from any thread; the mutation is marshalled onto the UI thread
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="logEvent" /> is null</exception>
    void Add(LogEvent logEvent);

    /// <summary>
    ///     Removes a log event from the collection
    /// </summary>
    /// <param name="logEvent">The log event to remove</param>
    bool Remove(LogEvent logEvent);
}