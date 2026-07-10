using System.Runtime.InteropServices;

namespace Focus.Services;

public sealed class SingleInstanceService : IDisposable
{
    // Local\ avoids admin requirements of Global\ and is enough for one user session.
    public const string MutexName = @"Local\JimmyB.Focus.SingleInstance";
    public const string ShowEventName = @"Local\JimmyB.Focus.ShowWindow";

    private Mutex? _mutex;
    private bool _owned;

    /// <summary>
    /// Try to become the only UI instance. Returns true if this process should show the main window.
    /// </summary>
    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            if (createdNew)
            {
                _owned = true;
                return true;
            }

            // Another process created it — try a non-blocking wait (also recovers abandoned mutex).
            try
            {
                if (_mutex.WaitOne(0))
                {
                    _owned = true;
                    return true;
                }
            }
            catch (AbandonedMutexException)
            {
                // Previous instance died without releasing — we now own it.
                _owned = true;
                return true;
            }

            try { _mutex.Dispose(); } catch { /* ignore */ }
            _mutex = null;
            return false;
        }
        catch (Exception)
        {
            // Never block the app if the OS refuses the mutex.
            _mutex = null;
            _owned = false;
            return true;
        }
    }

    /// <summary>Signal a running instance to show its window (best effort).</summary>
    public static void RequestShowExisting()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(ShowEventName);
            ev.Set();
        }
        catch
        {
            TryActivateExisting();
        }
    }

    /// <summary>Listen for RequestShowExisting from a second launch.</summary>
    public static EventWaitHandle CreateShowListener()
    {
        return new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
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
                {
                    // Hidden-to-tray windows sometimes report zero; still try enum.
                    continue;
                }

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
