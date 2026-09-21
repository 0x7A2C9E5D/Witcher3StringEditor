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
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if an update is available,
    ///     false otherwise
    /// </returns>
    Task<bool> CheckUpdate();
}