using System.Text;
using Focus.Core.Models;

namespace Focus.Core.Services.Messaging;

public static class TodoListFormatter
{
    public static string FormatSummary(
        string title,
        IEnumerable<TaskItem> tasks,
        IReadOnlyDictionary<string, string> folderNamesById)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(folderNamesById);

        var sb = new StringBuilder();
        sb.Append("FOCUS — ");
        sb.Append(title ?? "");
        sb.Append("\n\n");

        foreach (var task in tasks)
        {
            if (task is null)
                continue;
            if (task.Status != FocusTaskStatus.Open)
                continue;

            var folder = ResolveFolderName(task.FolderId, folderNamesById);
            sb.Append("• [");
            sb.Append(folder);
            sb.Append("] ");
            sb.Append(task.Title);

            var meta = FormatMeta(task);
            if (meta.Length > 0)
            {
                sb.Append(" (");
                sb.Append(meta);
                sb.Append(')');
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    public static string FormatReminder(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return $"⏰ FOCUS reminder: {task.Title}";
    }

    private static string ResolveFolderName(string? folderId, IReadOnlyDictionary<string, string> folderNamesById)
    {
        if (string.IsNullOrEmpty(folderId))
            return "?";
        return folderNamesById.TryGetValue(folderId, out var name) && !string.IsNullOrEmpty(name)
            ? name
            : folderId;
    }

    private static string FormatMeta(TaskItem task)
    {
        var parts = new List<string>(2);
        if (task.DueAtLocal is { } due)
            parts.Add($"due {due:g}");
        if (task.ReminderAtLocal is { } reminder)
            parts.Add($"reminder {reminder:g}");
        return string.Join(", ", parts);
    }
}
