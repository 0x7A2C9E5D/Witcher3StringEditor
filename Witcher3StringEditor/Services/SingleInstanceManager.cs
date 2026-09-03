using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Witcher3StringEditor.Services;

/// <summary>
///     Manages the single instance of the application
/// </summary>
/// <param name="isDebug"></param>
internal sealed class SingleInstanceManager(bool isDebug) : IDisposable
{
    private readonly string mutexName = isDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor";
    private bool disposedValue;
    private Mutex? mutex;

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
            mutex?.Dispose();
            mutex = null;
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