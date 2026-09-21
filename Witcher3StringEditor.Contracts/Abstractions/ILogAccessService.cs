using System.Collections.ObjectModel;
using Serilog.Events;

namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for logging access service operations
/// </summary>
public interface ILogAccessService
{
    /// <summary>
    ///     Gets the collection of logs
    /// </summary>
    public ObservableCollection<LogEvent> Logs { get; }
}