using System.Media;
using Focus.Core.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using Wpf = System.Windows;

namespace Focus.Services;

public sealed class NotificationService
{
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
            focusMainWindow?.Invoke();
    }

    public void ShowToast(string title, string body)
    {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "FOCUS" : title;
        var safeBody = string.IsNullOrWhiteSpace(body) ? "FOCUS reminder" : body;

        try
        {
            new ToastContentBuilder()
                .AddText(safeTitle)
                .AddText(safeBody)
                .Show();
        }
        catch
        {
            try
            {
                Wpf.MessageBox.Show(
                    safeBody,
                    safeTitle,
                    Wpf.MessageBoxButton.OK,
                    Wpf.MessageBoxImage.Information);
            }
            catch
            {
                // Headless / no UI thread.
            }
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
}
