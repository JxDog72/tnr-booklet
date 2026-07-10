using Focus.Core.Models;

namespace Focus.Core.Services;

/// <summary>
/// Keeps Windows Task Scheduler entries aligned with task reminder state.
/// </summary>
public sealed class SchedulerSyncService
{
    private readonly IReminderScheduler _scheduler;
    private readonly bool _enabled;
    private readonly bool _wake;

    public SchedulerSyncService(IReminderScheduler scheduler, bool enabled, bool wakeToRun)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _enabled = enabled;
        _wake = wakeToRun;
    }

    public void SyncTask(TaskItem task, string exePath)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!_enabled)
        {
            _scheduler.RemoveReminder(task.Id);
            return;
        }

        DateTime? when = task.Recurrence.IsRecurring
            ? task.Recurrence.NextFireAtLocal
            : task.ReminderAtLocal;

        if (task.Status == FocusTaskStatus.Done || when is null || when <= DateTime.Now.AddMinutes(-1))
        {
            _scheduler.RemoveReminder(task.Id);
            return;
        }

        _scheduler.UpsertReminder(task.Id, task.Title, when.Value, _wake, exePath);
    }
}
