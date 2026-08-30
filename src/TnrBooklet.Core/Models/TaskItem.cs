namespace Focus.Core.Models;

public sealed class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public string FolderId { get; set; } = "";
    public ItemKind Kind { get; set; } = ItemKind.Todo;
    public FocusTaskStatus Status { get; set; } = FocusTaskStatus.Open;
    public TaskPriority Priority { get; set; } = TaskPriority.None;
    public DateTime? DueAtLocal { get; set; }
    public DateTime? ReminderAtLocal { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public RecurrenceRule Recurrence { get; set; } = new();
    public List<string> TagIds { get; set; } = new();
    public int Progress { get; set; } = 1;
    public int SortOrder { get; set; }
}
