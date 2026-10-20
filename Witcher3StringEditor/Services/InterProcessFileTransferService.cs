using System.IO;
using System.IO.Pipes;
using Serilog;
using Witcher3StringEditor.Contracts.Abstractions;
using Witcher3StringEditor.Miscellaneous;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Transfers file paths between application instances over a named pipe
///     Lets a launch started through "Open with" hand its file to an already running instance
/// </summary>
internal sealed class InterProcessFileTransferService : IInterProcessFileTransfer, IDisposable
{
    private readonly string pipeName = DebugHelper.IsDebug
        ? "Witcher3StringEditor_Debug_Pipe"
        : "Witcher3StringEditor_Pipe"; // Pipe used to transfer files between instances

    private bool disposedValue;
    private Task? receiverTask;
    private CancellationTokenSource? receiverTokenSource;

    /// <summary>
    ///     Disposes of the resources used by the service
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Starts a background receiver that forwards file paths sent by later launches to this instance
    /// </summary>
    /// <param name="fileReceived">Callback invoked with the file path sent by another instance</param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void StartFileReceiver(Action<string> fileReceived)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        if (receiverTask is not null) return; // A receiver is already running
        receiverTokenSource = new CancellationTokenSource();
        var token = receiverTokenSource.Token;
        receiverTask = Task.Run(() => ReceiveFilesAsync(fileReceived, token), token);
    }

    /// <summary>
    ///     Sends a file path to an already running instance of the application
    /// </summary>
    /// <param name="filePath">The path of the file to open in the running instance</param>
    /// <returns>True when the running instance received the file path, false otherwise</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    public Task TrySendFileToExistingInstance(string filePath)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(2000); // Wait briefly in case the running instance is still starting its receiver
            using var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            writer.WriteLine(filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to forward file to the running instance: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Receives file paths over a named pipe and forwards them to the callback
    /// </summary>
    /// <param name="fileReceived">Callback invoked with each received file path</param>
    /// <param name="token">Token used to stop the receiver</param>
    private async Task ReceiveFilesAsync(Action<string> fileReceived, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
            try
            {
                await using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token);
                using var reader = new StreamReader(server);
                var filePath = await reader.ReadLineAsync(token);
                if (!string.IsNullOrWhiteSpace(filePath))
                    fileReceived(filePath);
            }
            catch (OperationCanceledException)
            {
                break; // The receiver was stopped during shutdown
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to receive a forwarded file");
            }
    }

    /// <summary>
    ///     Disposes of the resources used by the service
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    private void Dispose(bool disposing)
    {
        if (disposedValue) return;
        if (disposing)
        {
            receiverTokenSource?.Cancel(); // Stop the receiver loop
            receiverTokenSource?.Dispose();
        }

        disposedValue = true;
    }

    /// <summary>
    ///     Finalizes the service
    /// </summary>
    ~InterProcessFileTransferService()
    {
        Dispose(false);
    }
}