using Focus.Core.Recurrence;

namespace Focus.Core.Models;

public static class TaskProgress
{
    public const int Min = 1;
    public const int Max = 10;

    public static int Clamp(int value) => Math.Clamp(value, Min, Max);

    public static void ApplyTick(TaskItem task, int progress, DateTime nowLocal)
    {
        ArgumentNullException.ThrowIfNull(task);
        progress = Clamp(progress);
        if (progress == Max)
            CompleteFromProgress(task, nowLocal);
        else
            SetOpenProgress(task, progress);
    }

    public static void ApplyCheckbox(TaskItem task, DateTime nowLocal)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.Status == FocusTaskStatus.Done)
            SetOpenProgress(task, Max - 1);
        else
            CompleteFromProgress(task, nowLocal);
    }

    private static void CompleteFromProgress(TaskItem task, DateTime nowLocal)
    {
        ReminderAdvance.OnCompleted(task, nowLocal);
        task.Progress = task.Status == FocusTaskStatus.Done ? Max : Min;
    }

    private static void SetOpenProgress(TaskItem task, int progress)
    {
        task.Progress = Clamp(progress);
        task.Status = FocusTaskStatus.Open;
        task.CompletedAtUtc = null;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }
}
