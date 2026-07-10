using System.Windows;
using Focus.Core.Recurrence;
using Focus.Services;
using Focus.Themes;

namespace Focus;

public partial class App : System.Windows.Application
{
    private AppServices? _services;
    private SingleInstanceService? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args ?? Array.Empty<string>();
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
            await HandleRemindAsync(remindId);
            Shutdown();
            return;
        }

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            SingleInstanceService.TryActivateExisting();
            Shutdown();
            return;
        }

        var main = new MainWindow(_services);
        MainWindow = main;
        main.Show();
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

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
