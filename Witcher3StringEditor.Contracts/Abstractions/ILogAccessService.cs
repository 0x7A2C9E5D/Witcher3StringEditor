using System.Collections.ObjectModel;
using Serilog.Events;

namespace Witcher3StringEditor.Contracts.Abstractions;

public interface ILogAccessService
{
    public ObservableCollection<LogEvent> Logs { get; }
}