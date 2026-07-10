using Focus.Core.Data;
using Focus.Core.Services;
using Focus.Core.Services.Messaging;

namespace Focus.Services;

public sealed class AppServices : IDisposable
{
    private bool _disposed;

    public string DataDir { get; }
    public TaskStore Store { get; }
    public SettingsService Settings { get; }
    public ThemeFileService Themes { get; }
    public IReminderScheduler ReminderScheduler { get; }
    public ExportImportService ExportImport { get; } = new();

    public AppServices(string? dataDir = null)
    {
        DataDir = dataDir ?? DatabasePaths.GetDefaultDataDirectory();
        Directory.CreateDirectory(DataDir);

        Store = new TaskStore(DatabasePaths.GetDbPath(DataDir));
        Store.Initialize();

        Settings = new SettingsService(DatabasePaths.GetSettingsPath(DataDir));
        Settings.Load();

        Themes = new ThemeFileService(DatabasePaths.GetThemesPath(DataDir));
        Themes.LoadOrSeed();

        ReminderScheduler = new WindowsReminderScheduler();
    }

    /// <summary>Rebuilds scheduler sync from the latest settings flags.</summary>
    public SchedulerSyncService CreateSchedulerSync()
    {
        var s = Settings.Current;
        return new SchedulerSyncService(ReminderScheduler, s.TaskSchedulerEnabled, s.WakeToRun);
    }

    public MessagingService CreateMessaging() => new(Settings.Current);

    public string? GetExePath() =>
        Environment.ProcessPath
        ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

    public void Dispose()
    {
        if (_disposed) return;
        Store.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
