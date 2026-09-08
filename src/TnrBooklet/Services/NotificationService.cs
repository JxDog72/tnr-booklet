using System.Media;
using System.Runtime.InteropServices;
using Focus.Core.Models;
using Focus.Views;
using Microsoft.Toolkit.Uwp.Notifications;
using Wpf = System.Windows;

namespace Focus.Services;

public sealed class NotificationService
{
    public const string AppUserModelId = "JxDog72.TNRBooklet";

    public static void EnsureAppUserModelId()
    {
        try
        {
            _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch
        {
            // Toasts may still work; popup + sound are the backup.
        }
    }

    public ReminderNotifyResult Notify(TaskItem task, AppSettings settings, Action? focusMainWindow)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.NotificationsPaused)
            return ReminderNotifyResult.Dismissed;

        if (settings.ToastEnabled)
            ShowToast(task.Title, task.Notes);

        if (settings.SoundEnabled)
            PlaySound(settings.SoundPath);

        if (settings.PopupFocusEnabled)
        {
            try { focusMainWindow?.Invoke(); }
            catch { /* ignore */ }
            return ShowAlert(task);
        }

        if (!settings.ToastEnabled)
            return ShowAlert(task);

        return ReminderNotifyResult.Dismissed;
    }

    public void ShowToast(string title, string body)
    {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "TNR-Booklet" : title;
        var safeBody = string.IsNullOrWhiteSpace(body) ? "TNR-Booklet reminder" : body;

        try
        {
            new ToastContentBuilder()
                .AddText(safeTitle)
                .AddText(safeBody)
                .Show();
        }
        catch
        {
            // Popup in Notify is the backup.
        }
    }

    public static ReminderNotifyResult ShowAlert(TaskItem task)
    {
        var title = string.IsNullOrWhiteSpace(task.Title) ? "TNR-Booklet reminder" : task.Title;
        var body = string.IsNullOrWhiteSpace(task.Notes) ? "Reminder" : task.Notes;
        try
        {
            var app = Wpf.Application.Current;
            if (app is not null)
                return app.Dispatcher.Invoke(() => ShowAlertWindow(title, body));

            return ShowAlertWindow(title, body);
        }
        catch
        {
            try
            {
                Wpf.MessageBox.Show(body, title, Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Information);
            }
            catch
            {
                // Headless / no UI thread.
            }

            return ReminderNotifyResult.Dismissed;
        }
    }

    private static ReminderNotifyResult ShowAlertWindow(string title, string body)
    {
        var win = new ReminderAlertWindow(title, body);
        var app = Wpf.Application.Current;
        if (app?.MainWindow is { IsVisible: true } owner && !ReferenceEquals(owner, win))
        {
            win.Owner = owner;
            win.WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner;
        }

        win.ShowDialog();
        return win.SnoozeMinutes is int minutes
            ? ReminderNotifyResult.Snooze(minutes)
            : ReminderNotifyResult.Dismissed;
    }

    public void PlaySound(string? soundPath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(soundPath) && File.Exists(soundPath))
            {
                using var player = new SoundPlayer(soundPath);
                player.Play();
                return;
            }

            SystemSounds.Asterisk.Play();
        }
        catch
        {
            try { SystemSounds.Asterisk.Play(); } catch { /* ignore */ }
        }
    }

    public static void FocusMainWindow()
    {
        try
        {
            var app = Wpf.Application.Current;
            if (app is null) return;
            app.Dispatcher.Invoke(() =>
            {
                var win = app.MainWindow;
                if (win is null) return;
                if (!win.IsVisible)
                    win.Show();
                if (win.WindowState == Wpf.WindowState.Minimized)
                    win.WindowState = Wpf.WindowState.Normal;
                win.Activate();
                win.Topmost = true;
                win.Topmost = false;
                win.Focus();
            });
        }
        catch
        {
            // ignore
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);
}
