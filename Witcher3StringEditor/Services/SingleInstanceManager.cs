using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Witcher3StringEditor.Services;

internal sealed class SingleInstanceManager(bool isDebug) : IDisposable
{
    private readonly string mutexName = isDebug ? "Witcher3StringEditor_Debug" : "Witcher3StringEditor";
    private bool disposedValue;
    private Mutex? mutex;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool IsAnotherInstanceRunning()
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        mutex = new Mutex(true, mutexName, out var createdNew);
        return !createdNew;
    }

    public void ActivateExistingInstance()
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        using var existingProcess = FindExistingProcessInstance();
        if (existingProcess == null) return;
        var mainWindowHandle = new HWND(existingProcess.MainWindowHandle);
        ActivateExistingInstanceWindow(mainWindowHandle);
    }

    private static Process? FindExistingProcessInstance()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName);
        return processes.FirstOrDefault(p => p.Id != currentProcess.Id);
    }

    private static void ActivateExistingInstanceWindow(HWND mainWindowHandle)
    {
        var placement = new WINDOWPLACEMENT();
        placement.length = (uint)Marshal.SizeOf(placement);
        if (PInvoke.GetWindowPlacement(mainWindowHandle, ref placement).Value == 0) return;
        if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
            PInvoke.ShowWindow(mainWindowHandle, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(mainWindowHandle);
    }

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

    ~SingleInstanceManager()
    {
        Dispose(false);
    }
}