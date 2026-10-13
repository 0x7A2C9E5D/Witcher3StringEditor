using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Serilog;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Manages the single instance of the application
///     Detects whether an instance is already running, activates it, and forwards file paths
///     handed to a later launch (for example through "Open with") to that running instance
/// </summary>
/// <param name="isDebug"></param>
internal sealed class SingleInstanceManager(bool isDebug) : IDisposable
{
    private readonly string mutexName = isDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor";
    private readonly string pipeName = isDebug ? "Witcher3StringEditor_Debug_Pipe" : "Witcher3StringEditor_Pipe";
    private bool disposedValue;
    private Mutex? mutex;
    private Task? receiverTask;
    private CancellationTokenSource? receiverTokenSource;

    /// <summary>
    ///     Initializes a new instance of the SingleInstanceManager class
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Checks if another instance of the application is running
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ObjectDisposedException"></exception>
    public bool IsAnotherInstanceRunning()
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        mutex = new Mutex(true, mutexName, out var createdNew);
        return !createdNew;
    }

    /// <summary>
    ///     Activates an existing instance of the application
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    public void ActivateExistingInstance()
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        using var existingProcess = FindExistingProcessInstance();
        if (existingProcess == null) return;
        var mainWindowHandle = new HWND(existingProcess.MainWindowHandle);
        ActivateExistingInstanceWindow(mainWindowHandle);
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
    public bool TrySendFileToExistingInstance(string filePath)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(2000); // Wait briefly in case the running instance is still starting its receiver
            using var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            writer.WriteLine(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to forward file to the running instance: {FilePath}", filePath);
            return false;
        }
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
    ///     Finds an existing process instance of the application
    /// </summary>
    /// <returns></returns>
    private static Process? FindExistingProcessInstance()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName);
        return processes.FirstOrDefault(p => p.Id != currentProcess.Id);
    }

    /// <summary>
    ///     Activates an existing instance of the application
    /// </summary>
    /// <param name="mainWindowHandle"></param>
    private static void ActivateExistingInstanceWindow(HWND mainWindowHandle)
    {
        var placement = new WINDOWPLACEMENT();
        placement.length = (uint)Marshal.SizeOf(placement);
        if (PInvoke.GetWindowPlacement(mainWindowHandle, ref placement).Value == 0) return;
        if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
            PInvoke.ShowWindow(mainWindowHandle, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(mainWindowHandle);
    }

    /// <summary>
    ///     Disposes the resources used by the SingleInstanceManager class
    /// </summary>
    /// <param name="disposing"></param>
    private void Dispose(bool disposing)
    {
        if (disposedValue) return;
        if (disposing)
        {
            receiverTokenSource?.Cancel(); // Stop the receiver loop
            receiverTokenSource?.Dispose();
            mutex?.Dispose();
        }

        disposedValue = true;
    }

    /// <summary>
    ///     Finalizes the SingleInstanceManager class
    /// </summary>
    ~SingleInstanceManager()
    {
        Dispose(false);
    }
}