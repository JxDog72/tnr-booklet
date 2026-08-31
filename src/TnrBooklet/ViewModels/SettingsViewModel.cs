using System.Diagnostics;
using Focus.Core.Data;
using Focus.Core.Models;
using Focus.Services;
using Wpf = System.Windows;

namespace Focus.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private bool _toastEnabled;
    private bool _soundEnabled;
    private bool _popupFocusEnabled;
    private bool _trayEnabled;
    private bool _taskSchedulerEnabled;
    private bool _wakeToRun;
    private bool _closeToTray;
    private bool _notificationsPaused;
    private string _soundPath = "";
    private bool _telegramEnabled;
    private string _telegramBotToken = "";
    private string _telegramChatId = "";
    private bool _discordEnabled;
    private string _discordWebhookUrl = "";
    private bool _messagingOnReminder = true;
    private string _statusMessage = "";

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        LoadFrom(services.Settings.Current);
    }

    public bool ToastEnabled { get => _toastEnabled; set => SetProperty(ref _toastEnabled, value); }
    public bool SoundEnabled { get => _soundEnabled; set => SetProperty(ref _soundEnabled, value); }
    public bool PopupFocusEnabled { get => _popupFocusEnabled; set => SetProperty(ref _popupFocusEnabled, value); }
    public bool TrayEnabled { get => _trayEnabled; set => SetProperty(ref _trayEnabled, value); }
    public bool TaskSchedulerEnabled { get => _taskSchedulerEnabled; set => SetProperty(ref _taskSchedulerEnabled, value); }
    public bool WakeToRun { get => _wakeToRun; set => SetProperty(ref _wakeToRun, value); }
    public bool CloseToTray { get => _closeToTray; set => SetProperty(ref _closeToTray, value); }
    public bool NotificationsPaused { get => _notificationsPaused; set => SetProperty(ref _notificationsPaused, value); }
    public string SoundPath { get => _soundPath; set => SetProperty(ref _soundPath, value); }
    public bool TelegramEnabled { get => _telegramEnabled; set => SetProperty(ref _telegramEnabled, value); }
    public string TelegramBotToken { get => _telegramBotToken; set => SetProperty(ref _telegramBotToken, value); }
    public string TelegramChatId { get => _telegramChatId; set => SetProperty(ref _telegramChatId, value); }
    public bool DiscordEnabled { get => _discordEnabled; set => SetProperty(ref _discordEnabled, value); }
    public string DiscordWebhookUrl { get => _discordWebhookUrl; set => SetProperty(ref _discordWebhookUrl, value); }
    public bool MessagingOnReminder { get => _messagingOnReminder; set => SetProperty(ref _messagingOnReminder, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public string DataFolderPath => _services.DataDir;

    public string DataFolderHint => DatabasePaths.IsDefaultDirectory(_services.DataDir)
        ? "Default location on this PC."
        : "Custom folder. Pointer: %LocalAppData%\\TnrBooklet\\data-location.txt";

    public void LoadFrom(AppSettings s)
    {
        ToastEnabled = s.ToastEnabled;
        SoundEnabled = s.SoundEnabled;
        PopupFocusEnabled = s.PopupFocusEnabled;
        TrayEnabled = s.TrayEnabled;
        TaskSchedulerEnabled = s.TaskSchedulerEnabled;
        WakeToRun = s.WakeToRun;
        CloseToTray = s.CloseToTray;
        NotificationsPaused = s.NotificationsPaused;
        SoundPath = s.SoundPath ?? "";
        TelegramEnabled = s.TelegramEnabled;
        TelegramBotToken = s.TelegramBotToken ?? "";
        TelegramChatId = s.TelegramChatId ?? "";
        DiscordEnabled = s.DiscordEnabled;
        DiscordWebhookUrl = s.DiscordWebhookUrl ?? "";
        MessagingOnReminder = s.MessagingOnReminder;
    }

    public AppSettings ApplyTo(AppSettings s)
    {
        s.ToastEnabled = ToastEnabled;
        s.SoundEnabled = SoundEnabled;
        s.PopupFocusEnabled = PopupFocusEnabled;
        s.TrayEnabled = TrayEnabled;
        s.TaskSchedulerEnabled = TaskSchedulerEnabled;
        s.WakeToRun = WakeToRun;
        s.CloseToTray = CloseToTray;
        s.NotificationsPaused = NotificationsPaused;
        s.SoundPath = string.IsNullOrWhiteSpace(SoundPath) ? null : SoundPath.Trim();
        s.TelegramEnabled = TelegramEnabled;
        s.TelegramBotToken = string.IsNullOrWhiteSpace(TelegramBotToken) ? null : TelegramBotToken.Trim();
        s.TelegramChatId = string.IsNullOrWhiteSpace(TelegramChatId) ? null : TelegramChatId.Trim();
        s.DiscordEnabled = DiscordEnabled;
        s.DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl.Trim();
        s.MessagingOnReminder = MessagingOnReminder;
        return s;
    }

    public void Save()
    {
        ApplyTo(_services.Settings.Current);
        _services.Settings.Save();

        var sync = _services.CreateSchedulerSync();
        var exe = _services.GetExePath() ?? "";
        foreach (var task in _services.Store.QueryTasks(SmartView.All, null, null))
        {
            if (task.Status == FocusTaskStatus.Open)
                sync.SyncTask(task, exe);
        }

        StatusMessage = "Settings saved.";
    }

    public async Task TestMessagingAsync()
    {
        ApplyTo(_services.Settings.Current);
        var messaging = _services.CreateMessaging();
        var results = await messaging.SendAsync("TNR-Booklet test message ✅");
        if (results.Count == 0 || results.All(r => r.Error == "no channels"))
        {
            StatusMessage = "No messaging channels enabled/configured.";
            return;
        }

        if (results.All(r => r.Success))
            StatusMessage = "Test message sent.";
        else
            StatusMessage = "Send issues: " + string.Join("; ", results.Where(r => !r.Success).Select(r => r.Error ?? "error"));
    }

    public void OpenDataFolder()
    {
        var dir = _services.DataDir;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Switch data folder. Returns true if the app should restart.
    /// </summary>
    public bool RelocateTo(string newDir)
    {
        if (string.IsNullOrWhiteSpace(newDir))
            return false;

        string dest;
        try
        {
            dest = Path.GetFullPath(newDir.Trim());
        }
        catch (Exception ex)
        {
            StatusMessage = "Invalid folder: " + ex.Message;
            return false;
        }

        if (DatabasePaths.PathsEqual(dest, _services.DataDir))
        {
            StatusMessage = "Already using that folder.";
            return false;
        }

        Directory.CreateDirectory(dest);
        var destDb = Path.Combine(dest, "tnr-booklet.db");
        var destLegacy = Path.Combine(dest, "focus.db");
        var destHasData = File.Exists(destDb) || File.Exists(destLegacy);

        if (destHasData)
        {
            var useExisting = Wpf.MessageBox.Show(
                "This folder already has TNR-Booklet data.\n\nYes = switch to that data (files stay as they are).\nNo = cancel.",
                "TNR-Booklet",
                Wpf.MessageBoxButton.YesNo,
                Wpf.MessageBoxImage.Question);
            if (useExisting != Wpf.MessageBoxResult.Yes)
                return false;

            try
            {
                Save();
            }
            catch
            {
                // Current settings still apply until restart; pointer is the important write.
            }

            DatabasePaths.SetActiveDataDirectory(dest);
            StatusMessage = "Data folder updated. Restarting…";
            return true;
        }

        var copy = Wpf.MessageBox.Show(
            "Copy your current todos, notes, and settings to:\n\n" + dest + "\n\nThe app will restart.",
            "TNR-Booklet",
            Wpf.MessageBoxButton.YesNo,
            Wpf.MessageBoxImage.Question);
        if (copy != Wpf.MessageBoxResult.Yes)
            return false;

        try
        {
            Save();
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not save settings first: " + ex.Message;
            return false;
        }

        try
        {
            _services.Store.BackupTo(Path.Combine(dest, "tnr-booklet.db"));
            CopySidecarFiles(_services.DataDir, dest);
            DatabasePaths.SetActiveDataDirectory(dest);
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not copy data: " + ex.Message;
            return false;
        }

        StatusMessage = "Data folder updated. Restarting…";
        return true;
    }

    public bool ResetToDefault()
    {
        if (DatabasePaths.IsDefaultDirectory(_services.DataDir))
        {
            StatusMessage = "Already using the default folder.";
            return false;
        }

        var confirm = Wpf.MessageBox.Show(
            "Switch back to the default folder?\n\n" +
            DatabasePaths.GetAppRootDirectory() +
            "\n\nYour current custom folder is left as-is. The app will restart.",
            "TNR-Booklet",
            Wpf.MessageBoxButton.YesNo,
            Wpf.MessageBoxImage.Question);
        if (confirm != Wpf.MessageBoxResult.Yes)
            return false;

        DatabasePaths.SetActiveDataDirectory(null);
        StatusMessage = "Using default folder. Restarting…";
        return true;
    }

    private static void CopySidecarFiles(string fromDir, string toDir)
    {
        foreach (var name in new[] { "settings.json", "themes.json" })
        {
            var src = Path.Combine(fromDir, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(toDir, name), overwrite: true);
        }

        foreach (var src in Directory.GetFiles(fromDir, "last-fire-*.txt"))
            File.Copy(src, Path.Combine(toDir, Path.GetFileName(src)), overwrite: true);
    }
}
