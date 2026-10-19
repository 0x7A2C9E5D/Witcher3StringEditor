namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for transferring file paths between application instances over a named pipe
///     Lets a launch started through "Open with" hand its file to an already running instance
/// </summary>
public interface IInterProcessFileTransfer
{
    /// <summary>
    ///     Starts a background receiver that forwards file paths sent by later launches to this instance
    /// </summary>
    /// <param name="fileReceived">Callback invoked with the file path sent by another instance</param>
    public void StartFileReceiver(Action<string> fileReceived);

    /// <summary>
    ///     Sends a file path to an already running instance of the application
    /// </summary>
    /// <param name="filePath">The path of the file to open in the running instance</param>
    /// <returns>True when the running instance received the file path, false otherwise</returns>
    public Task TrySendFileToExistingInstance(string filePath);
}