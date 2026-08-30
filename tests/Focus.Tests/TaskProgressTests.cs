using Focus.Core.Models;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class TaskProgressTests
{
    [Fact]
    public void Tick_10_marks_one_shot_done()
    {
        var task = new TaskItem { Title = "One", Progress = 3 };
        TaskProgress.ApplyTick(task, 10, new DateTime(2026, 8, 24, 12, 0, 0));
        task.Status.Should().Be(FocusTaskStatus.Done);
        task.Progress.Should().Be(10);
        task.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Tick_4_reopens_done_item()
    {
        var task = new TaskItem
        {
            Title = "Done",
            Status = FocusTaskStatus.Done,
            Progress = 10,
            CompletedAtUtc = DateTime.UtcNow
        };
        TaskProgress.ApplyTick(task, 4, DateTime.Now);
        task.Status.Should().Be(FocusTaskStatus.Open);
        task.Progress.Should().Be(4);
        task.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Checkbox_off_sets_progress_9()
    {
        var task = new TaskItem
        {
            Title = "Done",
            Status = FocusTaskStatus.Done,
            Progress = 10,
            CompletedAtUtc = DateTime.UtcNow
        };
        TaskProgress.ApplyCheckbox(task, DateTime.Now);
        task.Status.Should().Be(FocusTaskStatus.Open);
        task.Progress.Should().Be(9);
    }

    [Fact]
    public void Tick_10_on_recurring_advances_and_resets_progress()
    {
        var task = new TaskItem
        {
            Title = "Recurring",
            Progress = 8,
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Daily,
                TimeOfDay = new TimeOnly(9, 0),
                IntervalN = 1,
                NextFireAtLocal = new DateTime(2026, 8, 24, 9, 0, 0)
            }
        };
        TaskProgress.ApplyTick(task, 10, new DateTime(2026, 8, 24, 12, 0, 0));
        task.Status.Should().Be(FocusTaskStatus.Open);
        task.Progress.Should().Be(1);
    }
}
