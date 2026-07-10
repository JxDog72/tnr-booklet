namespace Focus.Core.Models;

/// <summary>
/// Todo = actionable item (optional due/reminder).
/// Note = freeform note (no scheduler/reminder).
/// </summary>
public enum ItemKind
{
    Todo = 0,
    Note = 1
}
