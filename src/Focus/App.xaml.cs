using System.Windows;
using System.Windows.Threading;
using Focus.Core.Recurrence;
using Focus.Services;
using Focus.Themes;

namespace Focus;

public partial class App : System.Windows.Application
{
    private AppServices? _services;
    private SingleInstanceService? _singleInstance;
    private EventWaitHandle? _showListener;
    private Thread? _showListenerThread;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Surface crashes instead of silently closing.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            StartCore(e.Args ?? Array.Empty<string>());
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "TNR-Booklet failed to start:\n\n" + ex,
                "TNR-Booklet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void StartCore(string[] args)
    {
        var remindId = ParseRemindId(args);

        _services = new AppServices();

        try
        {
            var theme = _services.Themes.GetActive(_services.Settings.Current.ActiveThemeId);
            ThemeApplicator.Apply(theme);
        }
        catch
        {
            // Theme apply is best-effort at startup.
        }

        if (remindId is not null)
        {
            // Fire-and-forget path for Task Scheduler (no main window).
            _ = HandleRemindThenExitAsync(remindId);
            return;
        }

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            SingleInstanceService.RequestShowExisting();
            System.Windows.MessageBox.Show(
                "TNR-Booklet is already running.\n\nCheck the taskbar or the system tray (near the clock).",
                "TNR-Booklet",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Second-launch signal: show/restore main window.
        try
        {
            _showListener = SingleInstanceService.CreateShowListener();
            _showListenerThread = new Thread(ShowListenerLoop)
            {
                IsBackground = true,
                Name = "FOCUS.ShowListener"
            };
            _showListenerThread.Start();
        }
        catch
        {
            // Optional feature.
        }

        var main = new MainWindow(_services);
        MainWindow = main;
        main.Show();
        main.Activate();
        main.WindowState = WindowState.Normal;
    }

    private void ShowListenerLoop()
    {
        var handle = _showListener;
        if (handle is null) return;

        while (true)
        {
            try
            {
                handle.WaitOne();
                Dispatcher.BeginInvoke(() =>
                {
                    if (MainWindow is null) return;
                    MainWindow.Show();
                    MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Activate();
                });
            }
            catch
            {
                break;
            }
        }
    }

    private async Task HandleRemindThenExitAsync(string taskId)
    {
        try
        {
            await HandleRemindAsync(taskId);
        }
        catch (Exception ex)
        {
            try
            {
                System.Windows.MessageBox.Show("Reminder error:\n" + ex.Message, "TNR-Booklet");
            }
            catch { /* ignore */ }
        }
        finally
        {
            Shutdown();
        }
    }

    private async Task HandleRemindAsync(string taskId)
    {
        if (_services is null)
            return;

        if (WasRecentlyFired(_services.DataDir, taskId))
            return;

        var task = _services.Store.GetTask(taskId);
        if (task is null)
            return;

        MarkFired(_services.DataDir, taskId);

        var settings = _services.Settings.Current;
        var notify = new NotificationService();
        notify.Notify(task, settings, focusMainWindow: null);

        ReminderAdvance.OnFired(task, DateTime.Now);
        _services.Store.UpsertTask(task);

        var exe = _services.GetExePath() ?? "";
        try
        {
            _services.CreateSchedulerSync().SyncTask(task, exe);
        }
        catch
        {
            // Scheduler may be unavailable; still attempt messaging.
        }

        if (settings.MessagingOnReminder && !settings.NotificationsPaused)
        {
            try
            {
                var messaging = _services.CreateMessaging();
                await messaging.SendReminderAsync(task);
            }
            catch
            {
                // Messaging failures should not crash remind path.
            }
        }
    }

    private static string? ParseRemindId(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--remind", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
                && !string.IsNullOrWhiteSpace(args[i + 1]))
            {
                return args[i + 1].Trim();
            }
        }
        return null;
    }

    private static bool WasRecentlyFired(string dataDir, string taskId)
    {
        try
        {
            var path = GetLastFirePath(dataDir, taskId);
            if (!File.Exists(path))
                return false;
            var text = File.ReadAllText(path).Trim();
            if (!DateTime.TryParse(text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var when))
                return false;
            return (DateTime.UtcNow - when.ToUniversalTime()).TotalSeconds < 30;
        }
        catch
        {
            return false;
        }
    }

    private static void MarkFired(string dataDir, string taskId)
    {
        try
        {
            var path = GetLastFirePath(dataDir, taskId);
            File.WriteAllText(path, DateTime.UtcNow.ToString("o"));
        }
        catch
        {
            // ignore
        }
    }

    private static string GetLastFirePath(string dataDir, string taskId)
    {
        var safe = string.Concat(taskId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrEmpty(safe))
            safe = "unknown";
        return Path.Combine(dataDir, $"last-fire-{safe}.txt");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Unexpected error:\n\n" + e.Exception,
            "TNR-Booklet",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            System.Windows.MessageBox.Show(
                "Fatal error:\n\n" + e.ExceptionObject,
                "TNR-Booklet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* ignore */ }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _showListener?.Dispose(); } catch { /* ignore */ }
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
