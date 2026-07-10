using Focus.Core.Models;
using Focus.Core.Recurrence;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class ReminderAdvanceTests
{
    [Fact]
    public void OnFired_one_shot_clears_reminder()
    {
        var task = new TaskItem
        {
            ReminderAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.None,
                NextFireAtLocal = new DateTime(2026, 7, 10, 9, 0, 0)
            }
        };
        var before = task.UpdatedAtUtc;

        ReminderAdvance.OnFired(task, new DateTime(2026, 7, 10, 9, 0, 1));

        task.ReminderAtLocal.Should().BeNull();
        task.Recurrence.NextFireAtLocal.Should().BeNull();
        task.Status.Should().Be(FocusTaskStatus.Open);
        task.UpdatedAtUtc.Should().BeAfter(before);
    }

    [Fact]
    public void OnFired_recurring_advances_next_fire()
    {
        var task = new TaskItem
        {
            ReminderAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            DueAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Daily,
                TimeOfDay = new TimeOnly(9, 0),
                NextFireAtLocal = new DateTime(2026, 7, 10, 9, 0, 0)
            }
        };

        ReminderAdvance.OnFired(task, new DateTime(2026, 7, 10, 9, 0, 0));

        var expected = new DateTime(2026, 7, 11, 9, 0, 0);
        task.Recurrence.NextFireAtLocal.Should().Be(expected);
        task.ReminderAtLocal.Should().Be(expected);
        task.DueAtLocal.Should().Be(expected);
        task.Status.Should().Be(FocusTaskStatus.Open);
    }

    [Fact]
    public void OnFired_recurring_without_due_leaves_due_null()
    {
        var task = new TaskItem
        {
            ReminderAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            DueAtLocal = null,
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Daily,
                TimeOfDay = new TimeOnly(9, 0),
                NextFireAtLocal = new DateTime(2026, 7, 10, 9, 0, 0)
            }
        };

        ReminderAdvance.OnFired(task, new DateTime(2026, 7, 10, 9, 0, 0));

        task.DueAtLocal.Should().BeNull();
        task.ReminderAtLocal.Should().Be(new DateTime(2026, 7, 11, 9, 0, 0));
    }

    [Fact]
    public void OnCompleted_one_shot_marks_done()
    {
        var task = new TaskItem
        {
            Status = FocusTaskStatus.Open,
            ReminderAtLocal = new DateTime(2026, 7, 10, 17, 0, 0),
            Recurrence = new RecurrenceRule { Kind = RecurrenceKind.None }
        };

        ReminderAdvance.OnCompleted(task, new DateTime(2026, 7, 10, 12, 0, 0));

        task.Status.Should().Be(FocusTaskStatus.Done);
        task.CompletedAtUtc.Should().NotBeNull();
        task.ReminderAtLocal.Should().BeNull();
        task.Recurrence.NextFireAtLocal.Should().BeNull();
    }

    [Fact]
    public void OnCompleted_recurring_stays_open_and_advances()
    {
        var task = new TaskItem
        {
            Status = FocusTaskStatus.Open,
            ReminderAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            DueAtLocal = new DateTime(2026, 7, 10, 9, 0, 0),
            CompletedAtUtc = DateTime.UtcNow, // should be cleared
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Daily,
                TimeOfDay = new TimeOnly(9, 0),
                NextFireAtLocal = new DateTime(2026, 7, 10, 9, 0, 0)
            }
        };

        ReminderAdvance.OnCompleted(task, new DateTime(2026, 7, 10, 10, 0, 0));

        var expected = new DateTime(2026, 7, 11, 9, 0, 0);
        task.Status.Should().Be(FocusTaskStatus.Open);
        task.CompletedAtUtc.Should().BeNull();
        task.Recurrence.NextFireAtLocal.Should().Be(expected);
        task.ReminderAtLocal.Should().Be(expected);
        task.DueAtLocal.Should().Be(expected);
    }
}
