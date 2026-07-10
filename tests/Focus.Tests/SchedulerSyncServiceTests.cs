using Focus.Core.Models;
using Focus.Core.Services;
using Focus.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class SchedulerSyncServiceTests
{
    private const string ExePath = @"C:\Apps\Focus\Focus.exe";

    [Fact]
    public void SyncTask_when_disabled_removes_reminder()
    {
        var fake = new FakeReminderScheduler();
        fake.Existing.Add("t1");
        var sync = new SchedulerSyncService(fake, enabled: false, wakeToRun: true);
        var task = OpenTask("t1", reminder: DateTime.Now.AddHours(2));

        sync.SyncTask(task, ExePath);

        fake.Removals.Should().Contain("t1");
        fake.Upserts.Should().BeEmpty();
    }

    [Fact]
    public void SyncTask_when_open_with_reminder_upserts()
    {
        var fake = new FakeReminderScheduler();
        var sync = new SchedulerSyncService(fake, enabled: true, wakeToRun: true);
        var when = DateTime.Now.AddHours(3);
        var task = OpenTask("t2", reminder: when);
        task.Title = "Call bank";

        sync.SyncTask(task, ExePath);

        fake.Upserts.Should().ContainSingle();
        var call = fake.Upserts[0];
        call.TaskId.Should().Be("t2");
        call.Title.Should().Be("Call bank");
        call.NextFireLocal.Should().Be(when);
        call.WakeToRun.Should().BeTrue();
        call.ExePath.Should().Be(ExePath);
        fake.Removals.Should().BeEmpty();
        fake.Exists("t2").Should().BeTrue();
    }

    [Fact]
    public void SyncTask_when_done_removes_reminder()
    {
        var fake = new FakeReminderScheduler();
        fake.Existing.Add("t3");
        var sync = new SchedulerSyncService(fake, enabled: true, wakeToRun: false);
        var task = OpenTask("t3", reminder: DateTime.Now.AddHours(1));
        task.Status = FocusTaskStatus.Done;

        sync.SyncTask(task, ExePath);

        fake.Removals.Should().Contain("t3");
        fake.Upserts.Should().BeEmpty();
    }

    [Fact]
    public void SyncTask_when_no_reminder_removes()
    {
        var fake = new FakeReminderScheduler();
        var sync = new SchedulerSyncService(fake, enabled: true, wakeToRun: false);
        var task = OpenTask("t4", reminder: null);

        sync.SyncTask(task, ExePath);

        fake.Removals.Should().Contain("t4");
        fake.Upserts.Should().BeEmpty();
    }

    [Fact]
    public void SyncTask_recurring_uses_NextFireAtLocal()
    {
        var fake = new FakeReminderScheduler();
        var sync = new SchedulerSyncService(fake, enabled: true, wakeToRun: false);
        var next = DateTime.Now.AddDays(1).Date.AddHours(9);
        var task = new TaskItem
        {
            Id = "t5",
            Title = "Daily standup",
            Status = FocusTaskStatus.Open,
            ReminderAtLocal = DateTime.Now.AddHours(1), // should be ignored when recurring
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Daily,
                TimeOfDay = new TimeOnly(9, 0),
                NextFireAtLocal = next
            }
        };

        sync.SyncTask(task, ExePath);

        fake.Upserts.Should().ContainSingle();
        fake.Upserts[0].NextFireLocal.Should().Be(next);
    }

    private static TaskItem OpenTask(string id, DateTime? reminder) => new()
    {
        Id = id,
        Title = "Task",
        Status = FocusTaskStatus.Open,
        ReminderAtLocal = reminder,
        Recurrence = new RecurrenceRule { Kind = RecurrenceKind.None }
    };
}
