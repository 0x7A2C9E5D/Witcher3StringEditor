namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides application diagnostics functionality
/// </summary>
internal interface IAppDiagnostics
{
    /// <summary>
    ///     Logs information about the application startup
    /// </summary>
    /// <remarks>
    ///     Implementations must not throw. Diagnostics run synchronously on the startup critical path before
    ///     the main window is shown, so failures have to be logged and swallowed internally instead of
    ///     aborting the startup sequence
    /// </remarks>
    void LogStartupInfo();
}