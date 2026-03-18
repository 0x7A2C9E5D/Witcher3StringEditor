using System.Globalization;
using Serilog.Events;

namespace Witcher3StringEditor.Dialogs.Models;

/// <summary>
///     A model class that wraps a Serilog LogEvent for display in the UI
///     Provides easy access to log event properties such as timestamp, level, and message
/// </summary>
/// <param name="LogEvent">The Serilog LogEvent to wrap</param>
public record LogEventItemModel(LogEvent LogEvent)
{
    /// <summary>
    ///     Gets the original Serilog LogEvent
    /// </summary>
    public LogEvent EventEntry => LogEvent;

    /// <summary>
    ///     Gets the timestamp when the log event occurred
    /// </summary>
    public DateTimeOffset Timestamp => LogEvent.Timestamp;

    /// <summary>
    ///     Gets the level of the log event (e.g., Information, Warning, Error)
    /// </summary>
    public LogEventLevel Level => LogEvent.Level;

    /// <summary>
    ///     Gets the rendered message of the log event
    /// </summary>
    public string Message => LogEvent.RenderMessage(CultureInfo.InvariantCulture);
}