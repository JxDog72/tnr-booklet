using System.Runtime.InteropServices;

namespace Focus.Services;

public sealed class SingleInstanceService : IDisposable
{
    public const string MutexName = "Global\\JimmyB.Focus.SingleInstance";

    private Mutex? _mutex;
    private bool _owned;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _owned = createdNew;
        if (!createdNew)
        {
            try { _mutex.Dispose(); } catch { /* ignore */ }
            _mutex = null;
        }
        return createdNew;
    }

    /// <summary>Best-effort activate an existing FOCUS main window.</summary>
    public static void TryActivateExisting()
    {
        try
        {
            var current = Environment.ProcessPath;
            if (string.IsNullOrEmpty(current))
                return;

            var name = Path.GetFileNameWithoutExtension(current);
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName(name))
            {
                if (proc.Id == Environment.ProcessId)
                    continue;
                var hWnd = proc.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                    continue;
                ShowWindow(hWnd, SwRestore);
                SetForegroundWindow(hWnd);
                break;
            }
        }
        catch
        {
            // Best effort only.
        }
    }

    public void Dispose()
    {
        if (_mutex is null) return;
        try
        {
            if (_owned)
                _mutex.ReleaseMutex();
        }
        catch
        {
            // ignore
        }
        _mutex.Dispose();
        _mutex = null;
        _owned = false;
    }

    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
