using System.Globalization;
using Focus.Core.Models;

namespace Focus.Core.Recurrence;

public static class ReminderAdvance
{
    public const int MinSnoozeMinutes = 1;
    public const int MaxSnoozeMinutes = 10_080; // 7 days

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

    /// <summary>
    /// Delay this reminder by <paramref name="minutes"/> without advancing recurrence.
    /// </summary>
    public static void Snooze(TaskItem task, int minutes, DateTime nowLocal)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (minutes < MinSnoozeMinutes || minutes > MaxSnoozeMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutes),
                minutes,
                $"Snooze minutes must be between {MinSnoozeMinutes} and {MaxSnoozeMinutes}.");
        }

        task.ReminderAtLocal = nowLocal.AddMinutes(minutes);
        task.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static bool TryParseSnoozeMinutes(string? text, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            && !int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
        {
            return false;
        }

        if (n < MinSnoozeMinutes || n > MaxSnoozeMinutes)
            return false;

        minutes = n;
        return true;
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
