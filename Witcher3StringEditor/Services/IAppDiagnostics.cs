namespace Witcher3StringEditor.Services;

/// <summary>
///     Provides application diagnostics functionality
/// </summary>
internal interface IAppDiagnostics
{
    /// <summary>
    ///     Logs information about the application startup
    /// </summary>
    void LogStartupInfo();
}