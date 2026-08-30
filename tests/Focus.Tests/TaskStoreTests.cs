using Focus.Core.Data;
using Focus.Core.Models;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class TaskStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly TaskStore _store;

    public TaskStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "focus-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _store = new TaskStore(DatabasePaths.GetDbPath(_dir));
        _store.Initialize();
    }

    public void Dispose()
    {
        _store.Dispose();
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Initialize_seeds_work_and_personal()
    {
        var folders = _store.GetFolders();
        folders.Should().Contain(f => f.Name == "Work");
        folders.Should().Contain(f => f.Name == "Personal");
        folders.Should().Contain(f => f.Name == "Work" && f.Color == "#A78BFA");
        folders.Should().Contain(f => f.Name == "Personal" && f.Color == "#34D399");
    }

    [Fact]
    public void Insert_and_get_task_roundtrip()
    {
        var folder = _store.GetFolders().First();
        var task = new TaskItem
        {
            Title = "Ship report",
            FolderId = folder.Id,
            ReminderAtLocal = new DateTime(2026, 7, 11, 17, 0, 0),
            Recurrence = new RecurrenceRule
            {
                Kind = RecurrenceKind.Weekly,
                WeekdaysMask = RecurrenceRule.WeekdayBit(DayOfWeek.Friday),
                TimeOfDay = new TimeOnly(17, 0),
                NextFireAtLocal = new DateTime(2026, 7, 11, 17, 0, 0)
            }
        };
        _store.UpsertTask(task);
        var loaded = _store.GetTask(task.Id);
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Ship report");
        loaded.Recurrence.Kind.Should().Be(RecurrenceKind.Weekly);
        loaded.Recurrence.WeekdaysMask.Should().Be(RecurrenceRule.WeekdayBit(DayOfWeek.Friday));
        loaded.Recurrence.TimeOfDay.Should().Be(new TimeOnly(17, 0));
        loaded.ReminderAtLocal.Should().Be(task.ReminderAtLocal);
        loaded.Recurrence.NextFireAtLocal.Should().Be(task.Recurrence.NextFireAtLocal);
    }

    [Fact]
    public void Smart_view_today_filters()
    {
        var folder = _store.GetFolders().First();
        var today = DateTime.Today.AddHours(15);
        _store.UpsertTask(new TaskItem { Title = "Today", FolderId = folder.Id, DueAtLocal = today });
        _store.UpsertTask(new TaskItem { Title = "Later", FolderId = folder.Id, DueAtLocal = today.AddDays(3) });
        var list = _store.QueryTasks(SmartView.Today, null, null);
        list.Should().Contain(t => t.Title == "Today");
        list.Should().NotContain(t => t.Title == "Later");
    }

    [Fact]
    public void Progress_and_sort_order_roundtrip()
    {
        var folder = _store.GetFolders().First();
        var task = new TaskItem
        {
            Title = "Partial",
            FolderId = folder.Id,
            Progress = 7,
            SortOrder = 4
        };
        _store.UpsertTask(task);
        var loaded = _store.GetTask(task.Id);
        loaded.Should().NotBeNull();
        loaded!.Progress.Should().Be(7);
        loaded.SortOrder.Should().Be(4);
    }

    [Fact]
    public void Query_orders_by_sort_order()
    {
        var folder = _store.GetFolders().First();
        _store.UpsertTask(new TaskItem { Title = "Second", FolderId = folder.Id, SortOrder = 2 });
        _store.UpsertTask(new TaskItem { Title = "First", FolderId = folder.Id, SortOrder = 1 });
        var list = _store.QueryTasks(SmartView.All, folder.Id, null);
        list.Select(t => t.Title).Should().Equal("First", "Second");
    }

    [Fact]
    public void Reorder_visible_rewrites_sort_slots()
    {
        var folder = _store.GetFolders().First();
        var a = new TaskItem { Title = "A", FolderId = folder.Id, SortOrder = 0 };
        var b = new TaskItem { Title = "B", FolderId = folder.Id, SortOrder = 1 };
        var c = new TaskItem { Title = "C", FolderId = folder.Id, SortOrder = 2 };
        _store.UpsertTask(a);
        _store.UpsertTask(b);
        _store.UpsertTask(c);

        _store.ReorderVisible(new[] { b.Id, a.Id, c.Id });
        var list = _store.QueryTasks(SmartView.All, folder.Id, null);
        list.Select(t => t.Title).Should().Equal("B", "A", "C");
    }

    [Fact]
    public void NextSortOrder_increments()
    {
        var folder = _store.GetFolders().First();
        _store.NextSortOrder().Should().Be(0);
        _store.UpsertTask(new TaskItem { Title = "A", FolderId = folder.Id, SortOrder = 0 });
        _store.NextSortOrder().Should().Be(1);
    }
}
