using System.Collections.ObjectModel;
using Serilog.Events;
using Witcher3StringEditor.Contracts.Abstractions;

namespace Witcher3StringEditor.Services;

public class LogAccessService : ILogAccessService
{
    public ObservableCollection<LogEvent> Logs { get; } = [];
}