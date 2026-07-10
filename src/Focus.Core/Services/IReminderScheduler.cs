namespace Focus.Core.Services;

public interface IReminderScheduler
{
    void UpsertReminder(string taskId, string title, DateTime nextFireLocal, bool wakeToRun, string exePath);
    void RemoveReminder(string taskId);
    bool Exists(string taskId);
}
