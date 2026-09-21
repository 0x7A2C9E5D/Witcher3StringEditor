namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for update checking operations
///     Provides a method to check if an update is available for the application
/// </summary>
internal interface ICheckUpdateService
{
    /// <summary>
    ///     Checks if an update is available for the application
    /// </summary>
    /// <param name="cancellationToken">A token used to abort the update check</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if an update is
    ///     available, false when the application is up to date <b>or when the check failed</b>
    /// </returns>
    /// <remarks>
    ///     Failures are logged and reported as <c>false</c>: the result alone cannot distinguish "up to date"
    ///     from "check failed", so callers must treat the result as a best-effort hint only
    /// </remarks>
    Task<bool> CheckUpdate(CancellationToken cancellationToken = default);
}