namespace Witcher3StringEditor.Services;

/// <summary>
///     Defines a contract for playing the game
///     Provides a method to start the game process
/// </summary>
internal interface IPlayGameService
{
    /// <summary>
    ///     Starts the game process
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task PlayGame();
}