using System.Windows;
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
        IsReminder = !IsNote && task.ReminderAtLocal is not null;
        FolderColor = folder?.Color ?? "#A78BFA";
        FolderName = folder?.Name ?? "";
        Priority = task.Priority;
        Progress = TaskProgress.Clamp(task.Progress <= 0 ? TaskProgress.Min : task.Progress);
        SortOrder = task.SortOrder;
        IsOverdue = task.Kind == ItemKind.Todo
                    && task.Status == FocusTaskStatus.Open
                    && ((task.DueAtLocal is { } d && d.Date < DateTime.Today)
                        || (task.Recurrence.NextFireAtLocal is { } n && n.Date < DateTime.Today)
                        || (task.ReminderAtLocal is { } r && r.Date < DateTime.Today));
        IsRecurring = task.Recurrence.IsRecurring;
        Subtitle = BuildSubtitle(task, allTags);
        Ticks = Enumerable.Range(TaskProgress.Min, TaskProgress.Max)
            .Select(n => new ProgressTickVm(this, n, Progress, IsDone || Progress == TaskProgress.Max))
            .ToList();
    }

    public TaskItem Task { get; }
    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public bool IsDone { get; }
    public bool IsOverdue { get; }
    public bool IsRecurring { get; }
    public bool IsNote { get; }
    public bool IsReminder { get; }
    public ItemKind Kind { get; }
    public string KindLabel => IsNote ? "NOTE" : IsReminder ? "REMINDER" : "TODO";

    public Media.Brush KindBrush =>
        IsNote ? Frozen(0x2D, 0xD4, 0xBF)
        : IsReminder ? Frozen(0xFB, 0x71, 0x85)
        : Frozen(0xFB, 0xBF, 0x24);

    public Media.Brush KindBackground =>
        IsNote ? Frozen(0x0F, 0x2A, 0x28)
        : IsReminder ? Frozen(0x3F, 0x12, 0x19)
        : Frozen(0x2A, 0x22, 0x08);

    public string FolderColor { get; }
    public string FolderName { get; }
    public TaskPriority Priority { get; }
    public int Progress { get; }
    public int SortOrder { get; }
    public IReadOnlyList<ProgressTickVm> Ticks { get; }

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
        TaskPriority.High => "HIGH",
        TaskPriority.Medium => "Med",
        TaskPriority.Low => "Low",
        _ => ""
    };

    public Media.Brush PriorityBrush => Priority switch
    {
        TaskPriority.High => new Media.SolidColorBrush(Media.Color.FromRgb(0xF8, 0x71, 0x71)),
        TaskPriority.Medium => new Media.SolidColorBrush(Media.Color.FromRgb(0xFB, 0x92, 0x3C)),
        TaskPriority.Low => new Media.SolidColorBrush(Media.Color.FromRgb(0xE5, 0xE5, 0xE5)),
        _ => Media.Brushes.Transparent
    };

    public FontWeight PriorityWeight =>
        Priority == TaskPriority.High ? FontWeights.Bold : FontWeights.Normal;

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

    private static Media.SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new Media.SolidColorBrush(Media.Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}

public sealed class ProgressTickVm
{
    public ProgressTickVm(TaskListItemVm owner, int number, int progress, bool complete)
    {
        Owner = owner;
        Number = number;
        IsFilled = progress >= number;
        IsComplete = complete && IsFilled;
    }

    public TaskListItemVm Owner { get; }
    public int Number { get; }
    public bool IsFilled { get; }
    public bool IsComplete { get; }

    public Media.Brush FillBrush
    {
        get
        {
            if (IsComplete)
                return new Media.SolidColorBrush(Media.Color.FromRgb(0x34, 0xD3, 0x99));
            if (IsFilled)
                return new Media.SolidColorBrush(Media.Color.FromRgb(0xA7, 0x8B, 0xFA));
            return new Media.SolidColorBrush(Media.Color.FromRgb(0x1F, 0x1F, 0x24));
        }
    }
}
