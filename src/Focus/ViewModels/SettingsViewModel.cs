using Focus.Core.Models;
using Focus.Services;

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
        foreach (var task in _services.Store.QueryTasks(Core.Data.SmartView.All, null, null))
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
}
