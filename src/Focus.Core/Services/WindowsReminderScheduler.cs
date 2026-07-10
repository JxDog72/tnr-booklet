using Microsoft.Win32.TaskScheduler;

namespace Focus.Core.Services;

/// <summary>
/// Registers one-shot reminder tasks under the Windows Task Scheduler folder \Focus.
/// </summary>
public sealed class WindowsReminderScheduler : IReminderScheduler
{
    private const string FolderName = "Focus";

    public void UpsertReminder(string taskId, string title, DateTime nextFireLocal, bool wakeToRun, string exePath)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new ArgumentException("Task id is required.", nameof(taskId));
        if (string.IsNullOrWhiteSpace(exePath))
            throw new ArgumentException("Executable path is required.", nameof(exePath));

        try
        {
            using var ts = new TaskService();
            var folder = GetOrCreateFolder(ts);
            var name = TaskName(taskId);
            folder.DeleteTask(name, exceptionOnNotExists: false);

            var td = ts.NewTask();
            td.RegistrationInfo.Description = $"FOCUS reminder: {title}";
            td.Settings.WakeToRun = wakeToRun;
            td.Settings.StartWhenAvailable = true;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.Enabled = true;
            td.Triggers.Add(new TimeTrigger(nextFireLocal));
            td.Actions.Add(new ExecAction(exePath, $"--remind {taskId}", Path.GetDirectoryName(exePath)));
            folder.RegisterTaskDefinition(name, td);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            throw new InvalidOperationException(
                $"Failed to register FOCUS reminder for task '{taskId}': {ex.Message}", ex);
        }
    }

    public void RemoveReminder(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return;

        try
        {
            using var ts = new TaskService();
            var folder = TryGetFolder(ts);
            folder?.DeleteTask(TaskName(taskId), exceptionOnNotExists: false);
        }
        catch
        {
            // Do not crash callers when Task Scheduler is unavailable.
        }
    }

    public bool Exists(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return false;

        try
        {
            using var ts = new TaskService();
            var folder = TryGetFolder(ts);
            if (folder is null)
                return false;
            return folder.Tasks.Exists(TaskName(taskId));
        }
        catch
        {
            return false;
        }
    }

    private static string TaskName(string taskId) => $"remind-{taskId}";

    private static TaskFolder GetOrCreateFolder(TaskService ts)
    {
        if (ts.RootFolder.SubFolders.Exists(FolderName))
            return ts.RootFolder.SubFolders[FolderName];
        return ts.RootFolder.CreateFolder(FolderName);
    }

    private static TaskFolder? TryGetFolder(TaskService ts)
    {
        try
        {
            if (ts.RootFolder.SubFolders.Exists(FolderName))
                return ts.RootFolder.SubFolders[FolderName];
        }
        catch
        {
            // ignore
        }
        return null;
    }
}
