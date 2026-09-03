using System.Collections.ObjectModel;
using Serilog.Events;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides access to the application logs
/// </summary>
public class LogAccessService : ILogAccessService
{
    /// <summary>
    ///     Gets the collection of logs
    /// </summary>
    public ObservableCollection<LogEvent> Logs { get; } = [];
}