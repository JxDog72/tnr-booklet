using Focus.Core.Models;
using Focus.Themes;
using Media = System.Windows.Media;

namespace Focus.ViewModels;

public sealed class TaskListItemVm : ViewModelBase
{
    public TaskListItemVm(TaskItem task, Folder? folder, IReadOnlyList<Tag> allTags)
    {
        Task = task;
        Id = task.Id;
        Title = task.Title;
        IsDone = task.Status == FocusTaskStatus.Done;
        Kind = task.Kind;
        IsNote = task.Kind == ItemKind.Note;
        FolderColor = folder?.Color ?? "#A78BFA";
        FolderName = folder?.Name ?? "";
        Priority = task.Priority;
        IsOverdue = task.Kind == ItemKind.Todo
                    && task.Status == FocusTaskStatus.Open
                    && ((task.DueAtLocal is { } d && d.Date < DateTime.Today)
                        || (task.Recurrence.NextFireAtLocal is { } n && n.Date < DateTime.Today)
                        || (task.ReminderAtLocal is { } r && r.Date < DateTime.Today));
        IsRecurring = task.Recurrence.IsRecurring;
        Subtitle = BuildSubtitle(task, allTags);
    }

    public TaskItem Task { get; }
    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public bool IsDone { get; }
    public bool IsOverdue { get; }
    public bool IsRecurring { get; }
    public bool IsNote { get; }
    public ItemKind Kind { get; }
    public string KindLabel => IsNote ? "NOTE" : "TODO";
    public string FolderColor { get; }
    public string FolderName { get; }
    public TaskPriority Priority { get; }

    public Media.Brush FolderBrush
    {
        get
        {
            if (ThemeApplicator.TryParseColor(FolderColor, out var c))
                return new Media.SolidColorBrush(c);
            return new Media.SolidColorBrush(Media.Color.FromRgb(0xA7, 0x8B, 0xFA));
        }
    }

    public string PriorityLabel => Priority switch
    {
        TaskPriority.High => "High",
        TaskPriority.Medium => "Med",
        TaskPriority.Low => "Low",
        _ => ""
    };

    private static string BuildSubtitle(TaskItem task, IReadOnlyList<Tag> allTags)
    {
        var parts = new List<string>();

        if (task.Kind == ItemKind.Note)
        {
            parts.Add("Note");
            if (!string.IsNullOrWhiteSpace(task.Notes))
            {
                var preview = task.Notes.Replace("\r", " ").Replace("\n", " ").Trim();
                if (preview.Length > 60)
                    preview = preview[..60] + "…";
                if (preview.Length > 0)
                    parts.Add(preview);
            }
        }
        else
        {
            if (task.DueAtLocal is { } due)
                parts.Add($"Due {due:g}");
            else if (task.ReminderAtLocal is { } rem)
                parts.Add($"Reminder {rem:g}");
            else if (task.Recurrence.NextFireAtLocal is { } next)
                parts.Add($"Next {next:g}");

            if (task.Recurrence.IsRecurring)
                parts.Add(task.Recurrence.Kind.ToString());
        }

        if (task.TagIds.Count > 0)
        {
            var names = allTags
                .Where(t => task.TagIds.Contains(t.Id))
                .Select(t => "#" + t.Name)
                .ToList();
            if (names.Count > 0)
                parts.Add(string.Join(" ", names));
        }

        return string.Join(" · ", parts);
    }
}
