using System.Globalization;
using JetBrains.Annotations;
using Serilog.Events;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A model class that wraps a Serilog LogEvent for display in the UI
///     Provides easy access to log event properties such as timestamp, level, and message
/// </summary>
/// <param name="logEvent">The Serilog LogEvent to wrap</param>
public sealed class LogEventItemModel(LogEvent logEvent)
{
    /// <summary>
    ///     Gets the original Serilog LogEvent
    /// </summary>
    public LogEvent EventEntry { get; } = logEvent;

    /// <summary>
    ///     Gets the timestamp when the log event occurred
    /// </summary>
    [UsedImplicitly]
    public DateTimeOffset Timestamp => EventEntry.Timestamp;

    /// <summary>
    ///     Gets the level of the log event (e.g., Information, Warning, Error)
    /// </summary>
    [UsedImplicitly]
    public LogEventLevel Level => EventEntry.Level;

    /// <summary>
    ///     Gets the rendered message of the log event
    /// </summary>
    [UsedImplicitly]
    public string Message { get; } = logEvent.RenderMessage(CultureInfo.InvariantCulture);
}