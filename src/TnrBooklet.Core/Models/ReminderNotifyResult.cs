namespace Focus.Core.Models;

public readonly record struct ReminderNotifyResult(int? SnoozeMinutes)
{
    public bool IsSnooze => SnoozeMinutes is int minutes && minutes >= 1;

    public static ReminderNotifyResult Dismissed { get; } = new(null);

    public static ReminderNotifyResult Snooze(int minutes) => new(minutes);
}
