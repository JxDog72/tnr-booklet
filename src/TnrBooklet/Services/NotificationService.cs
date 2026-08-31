using System.Media;
using System.Runtime.InteropServices;
using Focus.Core.Models;
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

    public void Notify(TaskItem task, AppSettings settings, Action? focusMainWindow)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.NotificationsPaused)
            return;

        if (settings.ToastEnabled)
            ShowToast(task.Title, task.Notes);

        if (settings.SoundEnabled)
            PlaySound(settings.SoundPath);

        if (settings.PopupFocusEnabled)
        {
            try { focusMainWindow?.Invoke(); }
            catch { /* ignore */ }
            ShowAlert(task);
        }
        else if (!settings.ToastEnabled)
        {
            ShowAlert(task);
        }
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

    public static void ShowAlert(TaskItem task)
    {
        var title = string.IsNullOrWhiteSpace(task.Title) ? "TNR-Booklet reminder" : task.Title;
        var body = string.IsNullOrWhiteSpace(task.Notes) ? "Reminder" : task.Notes;
        try
        {
            var app = Wpf.Application.Current;
            if (app is not null)
            {
                app.Dispatcher.Invoke(() =>
                    Wpf.MessageBox.Show(body, title, Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Information));
            }
            else
            {
                Wpf.MessageBox.Show(body, title, Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Information);
            }
        }
        catch
        {
            // Headless / no UI thread.
        }
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
