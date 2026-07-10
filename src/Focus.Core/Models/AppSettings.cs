namespace Focus.Core.Models;

public sealed class AppSettings
{
    public bool ToastEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool PopupFocusEnabled { get; set; } = true;
    public bool TrayEnabled { get; set; } = true;
    public bool TaskSchedulerEnabled { get; set; } = true;
    public bool WakeToRun { get; set; } = false;
    public bool CloseToTray { get; set; } = true;
    public bool NotificationsPaused { get; set; } = false;
    public string? DefaultFolderId { get; set; }
    public string ActiveThemeId { get; set; } = "focus-dark";
    public string? SoundPath { get; set; }
    public bool SidebarCollapsed { get; set; } = false;
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 700;
}
