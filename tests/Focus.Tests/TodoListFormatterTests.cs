using Focus.Core.Models;
using Focus.Core.Services.Messaging;
using FluentAssertions;

namespace Focus.Tests;

public class TodoListFormatterTests
{
    [Fact]
    public void FormatSummary_includes_title_and_task_line()
    {
        var folderId = "folder-1";
        var folders = new Dictionary<string, string> { [folderId] = "Inbox" };
        var due = new DateTime(2026, 7, 10, 15, 0, 0);
        var reminder = new DateTime(2026, 7, 10, 14, 0, 0);

        var tasks = new[]
        {
            new TaskItem
            {
                Title = "Buy milk",
                FolderId = folderId,
                Status = FocusTaskStatus.Open,
                DueAtLocal = due,
                ReminderAtLocal = reminder
            },
            new TaskItem
            {
                Title = "Done already",
                FolderId = folderId,
                Status = FocusTaskStatus.Done
            }
        };

        var text = TodoListFormatter.FormatSummary("Today", tasks, folders);

        text.Should().StartWith("FOCUS — Today");
        text.Should().Contain("• [Inbox] Buy milk");
        text.Should().Contain("due");
        text.Should().Contain("reminder");
        text.Should().NotContain("Done already");
    }

    [Fact]
    public void FormatReminder_includes_task_title()
    {
        var task = new TaskItem { Title = "Call dentist" };
        TodoListFormatter.FormatReminder(task).Should().Be("⏰ FOCUS reminder: Call dentist");
    }
}
