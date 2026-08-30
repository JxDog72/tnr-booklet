using Focus.Core.Models;

namespace Focus.Core.Recurrence;

public static class ReminderAdvance
{
    /// <summary>After a reminder fires: reschedule recurring; clear one-shot reminder.</summary>
    public static void OnFired(TaskItem task, DateTime firedAtLocal)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.Recurrence.IsRecurring)
        {
            task.ReminderAtLocal = null;
            task.Recurrence.NextFireAtLocal = null;
            task.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        var next = RecurrenceCalculator.GetNextFireLocal(task.Recurrence, firedAtLocal);
        task.Recurrence.NextFireAtLocal = next;
        task.ReminderAtLocal = next;
        if (task.DueAtLocal is not null)
            task.DueAtLocal = next;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>User completed: one-shot → Done; recurring → advance, stay Open.</summary>
    public static void OnCompleted(TaskItem task, DateTime nowLocal)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.Recurrence.IsRecurring)
        {
            task.Status = FocusTaskStatus.Done;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.ReminderAtLocal = null;
            task.Recurrence.NextFireAtLocal = null;
            task.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        var next = RecurrenceCalculator.GetNextFireLocal(task.Recurrence, nowLocal);
        task.Status = FocusTaskStatus.Open;
        task.CompletedAtUtc = null;
        task.Recurrence.NextFireAtLocal = next;
        task.ReminderAtLocal = next;
        task.DueAtLocal = next;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }
}
