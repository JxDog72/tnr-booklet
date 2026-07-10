using Focus.Core.Services;

namespace Focus.Tests.Fakes;

public sealed class FakeReminderScheduler : IReminderScheduler
{
    public List<UpsertCall> Upserts { get; } = new();
    public List<string> Removals { get; } = new();
    public HashSet<string> Existing { get; } = new(StringComparer.Ordinal);

    public void UpsertReminder(string taskId, string title, DateTime nextFireLocal, bool wakeToRun, string exePath)
    {
        Upserts.Add(new UpsertCall(taskId, title, nextFireLocal, wakeToRun, exePath));
        Existing.Add(taskId);
        Removals.RemoveAll(id => id == taskId);
    }

    public void RemoveReminder(string taskId)
    {
        Removals.Add(taskId);
        Existing.Remove(taskId);
    }

    public bool Exists(string taskId) => Existing.Contains(taskId);

    public sealed record UpsertCall(
        string TaskId,
        string Title,
        DateTime NextFireLocal,
        bool WakeToRun,
        string ExePath);
}
