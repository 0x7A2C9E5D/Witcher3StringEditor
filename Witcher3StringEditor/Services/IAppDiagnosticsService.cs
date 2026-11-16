namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides application diagnostics functionality
/// </summary>
internal interface IAppDiagnosticsService
{
    /// <summary>
    ///     Logs information about the application startup
    /// </summary>
    void LogStartupInfo();
}