namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for playing the game
///     Provides a method to start the game process
/// </summary>
internal interface IPlayGameService
{
    /// <summary>
    ///     Starts the game process and waits for it to exit
    /// </summary>
    /// <param name="cancellationToken">A token used to abort waiting for the game process to exit</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    /// <exception cref="System.IO.FileNotFoundException">The configured game executable path is invalid</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">The game executable could not be started</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled</exception>
    Task PlayGame(CancellationToken cancellationToken = default);
}