# FOCUS Todo / Reminders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a local-first Windows WPF todo/reminder app (FOCUS) with folders, tags, smart views, deep theming, configurable notifications, and Windows Task Scheduler–backed reminders that work when the app is closed.

**Architecture:** Split pure logic into `Focus.Core` (models, recurrence, SQLite store, themes, export/import, scheduler abstractions). WPF app `Focus` owns UI, tray, toast, and composition root. Reminders register user-level Task Scheduler jobs that re-launch `Focus.exe --remind {id}`. Data lives under `%LocalAppData%\Focus\`.

**Tech Stack:** .NET 9 (`net9.0-windows`), C# WPF, Microsoft.Data.Sqlite, System.Text.Json, TaskScheduler (dahall) NuGet, Microsoft.Toolkit.Uwp.Notifications for toasts, xUnit + FluentAssertions for tests.

**Spec:** `docs/superpowers/specs/2026-07-10-focus-todo-reminders-design.md`

---

## File structure (create)

```
todoReminders/
  Focus.sln
  src/
    Focus.Core/
      Focus.Core.csproj
      Models/
        Folder.cs
        Tag.cs
        TaskItem.cs
        RecurrenceKind.cs
        RecurrenceRule.cs
        TaskPriority.cs
        TaskStatus.cs
        AppSettings.cs
        ThemeDefinition.cs
        ExportBundle.cs
      Data/
        DatabasePaths.cs
        Schema.sql          (embedded resource or const string)
        TaskStore.cs
      Recurrence/
        RecurrenceCalculator.cs
      Themes/
        ThemeCatalog.cs
        ThemeColors.cs
      Services/
        SettingsService.cs
        ThemeFileService.cs
        ExportImportService.cs
        IReminderScheduler.cs
        WindowsReminderScheduler.cs
        SchedulerSyncService.cs
    Focus/
      Focus.csproj
      App.xaml
      App.xaml.cs
      AssemblyInfo.cs
      MainWindow.xaml
      MainWindow.xaml.cs
      Views/
        TaskEditorWindow.xaml
        TaskEditorWindow.xaml.cs
        SettingsWindow.xaml
        SettingsWindow.xaml.cs
        ThemeEditorWindow.xaml
        ThemeEditorWindow.xaml.cs
      ViewModels/
        MainViewModel.cs
        TaskListItemVm.cs
        SidebarItemVm.cs
        TaskEditorViewModel.cs
        SettingsViewModel.cs
        ThemeEditorViewModel.cs
        RelayCommand.cs
        ViewModelBase.cs
      Services/
        NotificationService.cs
        TrayService.cs
        SingleInstanceService.cs
        AppServices.cs
      Themes/
        FocusDark.xaml
        ThemeApplicator.cs
      Converters/
        HexToBrushConverter.cs
        BoolToVisibilityConverter.cs
  tests/
    Focus.Tests/
      Focus.Tests.csproj
      RecurrenceCalculatorTests.cs
      TaskStoreTests.cs
      ExportImportServiceTests.cs
      ThemeCatalogTests.cs
      SchedulerSyncServiceTests.cs
  README.md
```

---

### Task 1: Solution scaffold

**Files:**
- Create: `Focus.sln`
- Create: `src/Focus.Core/Focus.Core.csproj`
- Create: `src/Focus/Focus.csproj`
- Create: `tests/Focus.Tests/Focus.Tests.csproj`
- Create: `src/Focus/App.xaml`, `src/Focus/App.xaml.cs`, `src/Focus/MainWindow.xaml`, `src/Focus/MainWindow.xaml.cs`
- Create: `README.md` (stub)

- [ ] **Step 1: Create solution and projects**

Run from repo root `todoReminders`:

```powershell
dotnet new sln -n Focus
dotnet new classlib -n Focus.Core -o src/Focus.Core -f net9.0
dotnet new wpf -n Focus -o src/Focus -f net9.0-windows
dotnet new xunit -n Focus.Tests -o tests/Focus.Tests -f net9.0
dotnet sln Focus.sln add src/Focus.Core/Focus.Core.csproj src/Focus/Focus.csproj tests/Focus.Tests/Focus.Tests.csproj
dotnet add src/Focus/Focus.csproj reference src/Focus.Core/Focus.Core.csproj
dotnet add tests/Focus.Tests/Focus.Tests.csproj reference src/Focus.Core/Focus.Core.csproj
dotnet add src/Focus.Core/Focus.Core.csproj package Microsoft.Data.Sqlite
dotnet add src/Focus.Core/Focus.Core.csproj package TaskScheduler
dotnet add src/Focus/Focus.csproj package Microsoft.Toolkit.Uwp.Notifications
dotnet add tests/Focus.Tests/Focus.Tests.csproj package FluentAssertions
dotnet add tests/Focus.Tests/Focus.Tests.csproj package Microsoft.Data.Sqlite
```

- [ ] **Step 2: Fix Focus.Core TFM if needed**

`Focus.Core` can stay `net9.0` (no WPF). Ensure `Focus.csproj` has:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net9.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UseWPF>true</UseWPF>
  <ApplicationIcon Condition="Exists('Assets/focus.ico')"></ApplicationIcon>
  <AssemblyName>Focus</AssemblyName>
  <RootNamespace>Focus</RootNamespace>
  <Product>FOCUS</Product>
</PropertyGroup>
```

Delete default `Class1.cs` from Focus.Core and default unit test placeholder content later.

- [ ] **Step 3: Build**

```powershell
dotnet build Focus.sln
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add Focus.sln src tests README.md
git commit -m "chore: scaffold Focus solution (Core, WPF, tests)"
```

---

### Task 2: Domain models

**Files:**
- Create: `src/Focus.Core/Models/*.cs` (all model types below)
- Delete: `src/Focus.Core/Class1.cs` if present

- [ ] **Step 1: Add enums and models**

`src/Focus.Core/Models/TaskStatus.cs`:

```csharp
namespace Focus.Core.Models;

public enum FocusTaskStatus
{
    Open = 0,
    Done = 1
}
```

`src/Focus.Core/Models/TaskPriority.cs`:

```csharp
namespace Focus.Core.Models;

public enum TaskPriority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
```

`src/Focus.Core/Models/RecurrenceKind.cs`:

```csharp
namespace Focus.Core.Models;

public enum RecurrenceKind
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    EveryNDays = 4
}
```

`src/Focus.Core/Models/RecurrenceRule.cs`:

```csharp
namespace Focus.Core.Models;

/// <summary>
/// Weekdays bitmask: bit0=Monday ... bit6=Sunday (ISO-like, Monday-first).
/// </summary>
public sealed class RecurrenceRule
{
    public RecurrenceKind Kind { get; set; } = RecurrenceKind.None;
    public int WeekdaysMask { get; set; }
    public TimeOnly TimeOfDay { get; set; } = new(9, 0);
    public int IntervalN { get; set; } = 1;
    public DateTime? NextFireAtLocal { get; set; }

    public bool IsRecurring => Kind != RecurrenceKind.None;

    public static int WeekdayBit(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => 1 << 0,
        DayOfWeek.Tuesday => 1 << 1,
        DayOfWeek.Wednesday => 1 << 2,
        DayOfWeek.Thursday => 1 << 3,
        DayOfWeek.Friday => 1 << 4,
        DayOfWeek.Saturday => 1 << 5,
        DayOfWeek.Sunday => 1 << 6,
        _ => 0
    };

    public bool IncludesWeekday(DayOfWeek dow) =>
        (WeekdaysMask & WeekdayBit(dow)) != 0;
}
```

`src/Focus.Core/Models/Folder.cs`:

```csharp
namespace Focus.Core.Models;

public sealed class Folder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#A78BFA";
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

`src/Focus.Core/Models/Tag.cs`:

```csharp
namespace Focus.Core.Models;

public sealed class Tag
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string? Color { get; set; }
}
```

`src/Focus.Core/Models/TaskItem.cs`:

```csharp
namespace Focus.Core.Models;

public sealed class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public string FolderId { get; set; } = "";
    public FocusTaskStatus Status { get; set; } = FocusTaskStatus.Open;
    public TaskPriority Priority { get; set; } = TaskPriority.None;
    public DateTime? DueAtLocal { get; set; }
    public DateTime? ReminderAtLocal { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public RecurrenceRule Recurrence { get; set; } = new();
    public List<string> TagIds { get; set; } = new();
}
```

`src/Focus.Core/Models/AppSettings.cs`:

```csharp
namespace Focus.Core.Models;

public sealed class AppSettings
{
    public bool ToastEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool PopupFocusEnabled { get; set; } = true;
    public bool TrayEnabled { get; set; } = true;
    public bool TaskSchedulerEnabled { get; set; } = true;
    public bool WakeToRun { get; set; } = false;
    public bool CloseToTray { get; set; } = true;
    public bool NotificationsPaused { get; set; } = false;
    public string? DefaultFolderId { get; set; }
    public string ActiveThemeId { get; set; } = "focus-dark";
    public string? SoundPath { get; set; }
    public bool SidebarCollapsed { get; set; } = false;
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 700;
}
```

`src/Focus.Core/Models/ThemeColors.cs` + theme definition:

```csharp
namespace Focus.Core.Models;

public sealed class ThemeColors
{
    public string BgApp { get; set; } = "#0A0A0C";
    public string BgSidebar { get; set; } = "#0E0E12";
    public string BgToolbar { get; set; } = "#121216";
    public string BgSurface { get; set; } = "#121216";
    public string BgSurfaceAlt { get; set; } = "#1A1A22";
    public string BorderDefault { get; set; } = "#1F1F24";
    public string BorderFocus { get; set; } = "#A78BFA";
    public string TextPrimary { get; set; } = "#E5E5E5";
    public string TextSecondary { get; set; } = "#9CA3AF";
    public string TextMuted { get; set; } = "#6B7280";
    public string Accent { get; set; } = "#A78BFA";
    public string Success { get; set; } = "#34D399";
    public string Warning { get; set; } = "#FBBF24";
    public string Danger { get; set; } = "#F87171";
    public string Overdue { get; set; } = "#F87171";
    public string SelectionBg { get; set; } = "#1A1528";
    public string SelectionFg { get; set; } = "#A78BFA";
}

public sealed class ThemeDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Custom";
    public ThemeColors Colors { get; set; } = new();
}
```

`src/Focus.Core/Models/ExportBundle.cs`:

```csharp
namespace Focus.Core.Models;

public sealed class ExportBundle
{
    public int Version { get; set; } = 1;
    public List<Folder> Folders { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
    public List<ThemeDefinition> Themes { get; set; } = new();
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Focus.sln
```

Expected: success.

- [ ] **Step 3: Commit**

```powershell
git add src/Focus.Core
git commit -m "feat(core): add domain models for tasks, themes, settings"
```

---

### Task 3: Recurrence calculator (TDD)

**Files:**
- Create: `src/Focus.Core/Recurrence/RecurrenceCalculator.cs`
- Create: `tests/Focus.Tests/RecurrenceCalculatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using Focus.Core.Models;
using Focus.Core.Recurrence;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class RecurrenceCalculatorTests
{
    [Fact]
    public void Daily_from_morning_returns_tomorrow_same_time_when_past()
    {
        var rule = new RecurrenceRule
        {
            Kind = RecurrenceKind.Daily,
            TimeOfDay = new TimeOnly(9, 0)
        };
        var from = new DateTime(2026, 7, 10, 10, 0, 0); // after 9am
        var next = RecurrenceCalculator.GetNextFireLocal(rule, from);
        next.Should().Be(new DateTime(2026, 7, 11, 9, 0, 0));
    }

    [Fact]
    public void Weekly_mon_wed_skips_to_wednesday()
    {
        var rule = new RecurrenceRule
        {
            Kind = RecurrenceKind.Weekly,
            TimeOfDay = new TimeOnly(9, 0),
            WeekdaysMask = RecurrenceRule.WeekdayBit(DayOfWeek.Monday)
                          | RecurrenceRule.WeekdayBit(DayOfWeek.Wednesday)
        };
        // Tuesday 2026-07-07
        var from = new DateTime(2026, 7, 7, 8, 0, 0);
        var next = RecurrenceCalculator.GetNextFireLocal(rule, from);
        next.Should().Be(new DateTime(2026, 7, 8, 9, 0, 0)); // Wednesday
    }

    [Fact]
    public void Every_n_days_adds_interval()
    {
        var rule = new RecurrenceRule
        {
            Kind = RecurrenceKind.EveryNDays,
            IntervalN = 3,
            TimeOfDay = new TimeOnly(14, 30)
        };
        var from = new DateTime(2026, 7, 10, 15, 0, 0);
        var next = RecurrenceCalculator.GetNextFireLocal(rule, from);
        next.Should().Be(new DateTime(2026, 7, 13, 14, 30, 0));
    }

    [Fact]
    public void Monthly_same_day_next_month()
    {
        var rule = new RecurrenceRule
        {
            Kind = RecurrenceKind.Monthly,
            TimeOfDay = new TimeOnly(8, 0)
        };
        var from = new DateTime(2026, 1, 31, 9, 0, 0);
        var next = RecurrenceCalculator.GetNextFireLocal(rule, from);
        // Feb has no 31 → clamp to last day of Feb
        next.Should().Be(new DateTime(2026, 2, 28, 8, 0, 0));
    }

    [Fact]
    public void None_returns_null()
    {
        var rule = new RecurrenceRule { Kind = RecurrenceKind.None };
        RecurrenceCalculator.GetNextFireLocal(rule, DateTime.Now).Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

```powershell
dotnet test tests/Focus.Tests/Focus.Tests.csproj --filter FullyQualifiedName~RecurrenceCalculatorTests
```

Expected: FAIL (type or method missing).

- [ ] **Step 3: Implement calculator**

```csharp
namespace Focus.Core.Recurrence;

using Focus.Core.Models;

public static class RecurrenceCalculator
{
    /// <summary>
    /// Next fire strictly after <paramref name="fromLocal"/> (local wall time).
    /// </summary>
    public static DateTime? GetNextFireLocal(RecurrenceRule rule, DateTime fromLocal)
    {
        if (rule.Kind == RecurrenceKind.None) return null;
        var time = rule.TimeOfDay;
        var interval = Math.Max(1, rule.IntervalN);

        return rule.Kind switch
        {
            RecurrenceKind.Daily => NextDaily(fromLocal, time),
            RecurrenceKind.Weekly => NextWeekly(fromLocal, time, rule.WeekdaysMask),
            RecurrenceKind.Monthly => NextMonthly(fromLocal, time),
            RecurrenceKind.EveryNDays => NextEveryNDays(fromLocal, time, interval),
            _ => null
        };
    }

    private static DateTime AtTime(DateTime date, TimeOnly time) =>
        date.Date + time.ToTimeSpan();

    private static DateTime NextDaily(DateTime from, TimeOnly time)
    {
        var candidate = AtTime(from, time);
        if (candidate > from) return candidate;
        return AtTime(from.Date.AddDays(1), time);
    }

    private static DateTime NextWeekly(DateTime from, TimeOnly time, int mask)
    {
        if (mask == 0)
            mask = RecurrenceRule.WeekdayBit(from.DayOfWeek);

        for (var i = 0; i < 8; i++)
        {
            var day = from.Date.AddDays(i);
            if (!Includes(mask, day.DayOfWeek)) continue;
            var candidate = AtTime(day, time);
            if (candidate > from) return candidate;
        }
        // fallback next week first matching
        for (var i = 1; i <= 7; i++)
        {
            var day = from.Date.AddDays(7 + i);
            if (!Includes(mask, day.DayOfWeek)) continue;
            return AtTime(day, time);
        }
        return AtTime(from.Date.AddDays(7), time);
    }

    private static bool Includes(int mask, DayOfWeek dow) =>
        (mask & RecurrenceRule.WeekdayBit(dow)) != 0;

    private static DateTime NextMonthly(DateTime from, TimeOnly time)
    {
        var day = from.Day;
        var y = from.Year;
        var m = from.Month;
        var candidate = SafeDate(y, m, day, time);
        if (candidate > from) return candidate;
        m++;
        if (m > 12) { m = 1; y++; }
        return SafeDate(y, m, day, time);
    }

    private static DateTime SafeDate(int y, int m, int day, TimeOnly time)
    {
        var dim = DateTime.DaysInMonth(y, m);
        var d = Math.Min(day, dim);
        return new DateTime(y, m, d) + time.ToTimeSpan();
    }

    private static DateTime NextEveryNDays(DateTime from, TimeOnly time, int n)
    {
        var candidate = AtTime(from, time);
        if (candidate > from) return candidate;
        return AtTime(from.Date.AddDays(n), time);
    }
}
```

- [ ] **Step 4: Run tests — expect pass**

```powershell
dotnet test tests/Focus.Tests/Focus.Tests.csproj --filter FullyQualifiedName~RecurrenceCalculatorTests
```

Expected: all passed.

- [ ] **Step 5: Commit**

```powershell
git add src/Focus.Core/Recurrence tests/Focus.Tests/RecurrenceCalculatorTests.cs
git commit -m "feat(core): recurrence next-fire calculator with tests"
```

---

### Task 4: Database paths + TaskStore (TDD)

**Files:**
- Create: `src/Focus.Core/Data/DatabasePaths.cs`
- Create: `src/Focus.Core/Data/TaskStore.cs`
- Create: `tests/Focus.Tests/TaskStoreTests.cs`

- [ ] **Step 1: DatabasePaths**

```csharp
namespace Focus.Core.Data;

public static class DatabasePaths
{
    public static string GetDefaultDataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Focus");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetDbPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "focus.db");

    public static string GetSettingsPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "settings.json");

    public static string GetThemesPath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDefaultDataDirectory(), "themes.json");
}
```

- [ ] **Step 2: Failing store tests** (use temp directory)

```csharp
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
        loaded.ReminderAtLocal.Should().Be(task.ReminderAtLocal);
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
}
```

Also add to Core:

```csharp
namespace Focus.Core.Data;

public enum SmartView
{
    All,
    Today,
    Upcoming,
    Overdue,
    Completed
}
```

- [ ] **Step 3: Run — expect fail**

```powershell
dotnet test tests/Focus.Tests --filter FullyQualifiedName~TaskStoreTests
```

- [ ] **Step 4: Implement TaskStore**

Implement `TaskStore` with:

- Constructor `(string dbPath)`
- `Initialize()` — create schema, seed Work/Personal if empty
- `Dispose()` — close connection
- CRUD: `GetFolders`, `UpsertFolder`, `DeleteFolder`, `GetTags`, `UpsertTag`, `DeleteTag`, `GetTask`, `UpsertTask`, `DeleteTask`, `QueryTasks(SmartView view, string? folderId, string? tagId)`
- Schema SQL (inline string):

```sql
CREATE TABLE IF NOT EXISTS folders (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  color TEXT NOT NULL,
  sort_order INTEGER NOT NULL,
  created_at_utc TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS tags (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL UNIQUE,
  color TEXT
);
CREATE TABLE IF NOT EXISTS tasks (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  notes TEXT NOT NULL DEFAULT '',
  folder_id TEXT NOT NULL,
  status INTEGER NOT NULL,
  priority INTEGER NOT NULL,
  due_at_local TEXT,
  reminder_at_local TEXT,
  completed_at_utc TEXT,
  created_at_utc TEXT NOT NULL,
  updated_at_utc TEXT NOT NULL,
  rec_kind INTEGER NOT NULL DEFAULT 0,
  rec_weekdays INTEGER NOT NULL DEFAULT 0,
  rec_time TEXT NOT NULL DEFAULT '09:00',
  rec_interval INTEGER NOT NULL DEFAULT 1,
  rec_next_fire_local TEXT,
  FOREIGN KEY(folder_id) REFERENCES folders(id)
);
CREATE TABLE IF NOT EXISTS task_tags (
  task_id TEXT NOT NULL,
  tag_id TEXT NOT NULL,
  PRIMARY KEY(task_id, tag_id)
);
```

Store datetimes as ISO-8601 local strings (`"o"` round-trip or `yyyy-MM-ddTHH:mm:ss`). Use `Microsoft.Data.Sqlite`. Keep a single connection open for the store lifetime.

Query rules:

- **Today:** `status=Open` and (`due_at` date == today OR `reminder` date == today OR `rec_next_fire` date == today)
- **Upcoming:** open, future due/reminder after today end
- **Overdue:** open, due/reminder/next_fire date &lt; today
- **Completed:** status=Done
- **All:** no status filter (or open+done depending on UI flag; default all open unless Completed view)

- [ ] **Step 5: Tests pass + commit**

```powershell
dotnet test tests/Focus.Tests --filter FullyQualifiedName~TaskStoreTests
git add src/Focus.Core/Data tests/Focus.Tests/TaskStoreTests.cs
git commit -m "feat(core): SQLite TaskStore with seeds and smart views"
```

---

### Task 5: Settings, themes, export/import (TDD)

**Files:**
- Create: `src/Focus.Core/Themes/ThemeCatalog.cs`
- Create: `src/Focus.Core/Services/SettingsService.cs`
- Create: `src/Focus.Core/Services/ThemeFileService.cs`
- Create: `src/Focus.Core/Services/ExportImportService.cs`
- Create: `tests/Focus.Tests/ThemeCatalogTests.cs`
- Create: `tests/Focus.Tests/ExportImportServiceTests.cs`

- [ ] **Step 1: ThemeCatalog built-in Focus Dark**

```csharp
namespace Focus.Core.Themes;

using Focus.Core.Models;

public static class ThemeCatalog
{
    public const string FocusDarkId = "focus-dark";

    public static ThemeDefinition CreateFocusDark() => new()
    {
        Id = FocusDarkId,
        Name = "Focus Dark",
        Colors = new ThemeColors() // defaults already Focus Dark
    };

    public static ThemeDefinition EnsureDefaults(ThemeDefinition? theme)
    {
        if (theme is null) return CreateFocusDark();
        theme.Colors ??= new ThemeColors();
        return theme;
    }
}
```

- [ ] **Step 2: SettingsService / ThemeFileService**

JSON via `System.Text.Json` with camelCase. Load or create defaults. Atomic write: write temp then replace.

- [ ] **Step 3: ExportImportService**

```csharp
public sealed class ExportImportService
{
    public ExportBundle Export(TaskStore store, AppSettings settings, IReadOnlyList<ThemeDefinition> themes) { /* map all */ }
    public void ImportReplace(TaskStore store, SettingsService settingsService, ThemeFileService themeService, ExportBundle bundle)
    {
        // validate Version >= 1, non-null collections
        // clear and reinsert folders/tags/tasks
        // save settings + themes
    }
}
```

Validation: throw `InvalidDataException` with message if `bundle.Folders` null or any task missing title/folder.

- [ ] **Step 4: Tests**

- ThemeCatalog default id is `focus-dark`, accent `#A78BFA`
- Export → ImportReplace round-trip preserves task title and folder count
- Corrupt bundle (null tasks) throws

- [ ] **Step 5: Commit**

```powershell
dotnet test tests/Focus.Tests
git add src/Focus.Core tests/Focus.Tests
git commit -m "feat(core): settings, themes, export/import"
```

---

### Task 6: Reminder scheduler bridge

**Files:**
- Create: `src/Focus.Core/Services/IReminderScheduler.cs`
- Create: `src/Focus.Core/Services/WindowsReminderScheduler.cs`
- Create: `src/Focus.Core/Services/SchedulerSyncService.cs`
- Create: `tests/Focus.Tests/SchedulerSyncServiceTests.cs`
- Create: `tests/Focus.Tests/Fakes/FakeReminderScheduler.cs`

- [ ] **Step 1: Interfaces**

```csharp
namespace Focus.Core.Services;

public interface IReminderScheduler
{
    void UpsertReminder(string taskId, string title, DateTime nextFireLocal, bool wakeToRun);
    void RemoveReminder(string taskId);
    bool Exists(string taskId);
}

public sealed class SchedulerSyncService
{
    private readonly IReminderScheduler _scheduler;
    private readonly bool _enabled;
    private readonly bool _wake;

    public SchedulerSyncService(IReminderScheduler scheduler, bool enabled, bool wakeToRun)
    {
        _scheduler = scheduler;
        _enabled = enabled;
        _wake = wakeToRun;
    }

    public void SyncTask(TaskItem task, string exePath)
    {
        if (!_enabled)
        {
            _scheduler.RemoveReminder(task.Id);
            return;
        }

        DateTime? when = task.Recurrence.IsRecurring
            ? task.Recurrence.NextFireAtLocal
            : task.ReminderAtLocal;

        if (task.Status == FocusTaskStatus.Done || when is null || when <= DateTime.Now.AddMinutes(-1))
        {
            _scheduler.RemoveReminder(task.Id);
            return;
        }

        _scheduler.UpsertReminder(task.Id, task.Title, when.Value, _wake);
    }
}
```

- [ ] **Step 2: Fake + unit tests** for SyncTask enable/disable/done/remove

- [ ] **Step 3: WindowsReminderScheduler** using `Microsoft.Win32.TaskScheduler`:

- Task folder `\Focus`
- Task name `remind-{taskId}`
- Action: `exePath` with arguments `--remind {taskId}`
- Trigger: one-shot at `nextFireLocal` (for recurring, app reschedules after fire)
- `task.Settings.WakeToRun = wakeToRun`
- `task.Settings.StartWhenAvailable = true` (catch up if PC was sleeping)
- Run only when user is logged on

```csharp
using Microsoft.Win32.TaskScheduler;

public sealed class WindowsReminderScheduler : IReminderScheduler
{
    private const string FolderName = "Focus";

    public void UpsertReminder(string taskId, string title, DateTime nextFireLocal, bool wakeToRun)
    {
        using var ts = new TaskService();
        var folder = ts.RootFolder.SubFolders.Exists(FolderName)
            ? ts.RootFolder.SubFolders[FolderName]
            : ts.RootFolder.CreateFolder(FolderName);

        var name = $"remind-{taskId}";
        folder.DeleteTask(name, exceptionOnNotExists: false);

        var td = ts.NewTask();
        td.RegistrationInfo.Description = $"FOCUS reminder: {title}";
        td.Settings.WakeToRun = wakeToRun;
        td.Settings.StartWhenAvailable = true;
        td.Settings.DisallowStartIfOnBatteries = false;
        td.Settings.StopIfGoingOnBatteries = false;
        td.Triggers.Add(new TimeTrigger(nextFireLocal));
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("No process path");
        td.Actions.Add(new ExecAction(exe, $"--remind {taskId}", Path.GetDirectoryName(exe)));
        folder.RegisterTaskDefinition(name, td);
    }

    public void RemoveReminder(string taskId) { /* delete if exists */ }
    public bool Exists(string taskId) { /* ... */ }
}
```

Note: For unit tests never hit real Task Scheduler; only Fake.

- [ ] **Step 4: Commit**

```powershell
dotnet test tests/Focus.Tests
git add src/Focus.Core/Services tests/Focus.Tests
git commit -m "feat(core): Task Scheduler sync for reminders"
```

---

### Task 7: Advance recurrence helpers

**Files:**
- Create: `src/Focus.Core/Recurrence/ReminderAdvance.cs`
- Create: `tests/Focus.Tests/ReminderAdvanceTests.cs`

- [ ] **Step 1: Spec rules as code**

```csharp
namespace Focus.Core.Recurrence;

using Focus.Core.Models;

public static class ReminderAdvance
{
    /// <summary>After a reminder fires: reschedule recurring; clear one-shot reminder.</summary>
    public static void OnFired(TaskItem task, DateTime firedAtLocal)
    {
        if (!task.Recurrence.IsRecurring)
        {
            task.ReminderAtLocal = null;
            task.Recurrence.NextFireAtLocal = null;
            task.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        var next = RecurrenceCalculator.GetNextFireLocal(task.Recurrence, firedAtLocal);
        task.Recurrence.NextFireAtLocal = next;
        task.ReminderAtLocal = next;
        if (task.DueAtLocal is not null)
            task.DueAtLocal = next;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>User completed: one-shot → Done; recurring → advance, stay Open.</summary>
    public static void OnCompleted(TaskItem task, DateTime nowLocal)
    {
        if (!task.Recurrence.IsRecurring)
        {
            task.Status = FocusTaskStatus.Done;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.ReminderAtLocal = null;
            task.Recurrence.NextFireAtLocal = null;
            task.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        var next = RecurrenceCalculator.GetNextFireLocal(task.Recurrence, nowLocal);
        task.Status = FocusTaskStatus.Open;
        task.CompletedAtUtc = null;
        task.Recurrence.NextFireAtLocal = next;
        task.ReminderAtLocal = next;
        task.DueAtLocal = next;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 2: Tests for both paths + commit**

```powershell
dotnet test tests/Focus.Tests
git add src/Focus.Core/Recurrence tests/Focus.Tests/ReminderAdvanceTests.cs
git commit -m "feat(core): advance rules on fire and complete"
```

---

### Task 8: WPF infrastructure (VM base, commands, theme apply)

**Files:**
- Create: `src/Focus/ViewModels/ViewModelBase.cs`, `RelayCommand.cs`
- Create: `src/Focus/Themes/FocusDark.xaml`, `ThemeApplicator.cs`
- Create: `src/Focus/Services/AppServices.cs`
- Modify: `src/Focus/App.xaml`, `App.xaml.cs`

- [ ] **Step 1: ViewModelBase + RelayCommand** (INotifyPropertyChanged, ICommand)

- [ ] **Step 2: FocusDark.xaml resource keys**

Define brushes matching ThemeColors property names:

```xml
<SolidColorBrush x:Key="BgAppBrush" Color="#0A0A0C"/>
<SolidColorBrush x:Key="BgSidebarBrush" Color="#0E0E12"/>
<!-- ... all tokens ... -->
```

- [ ] **Step 3: ThemeApplicator**

Map `ThemeColors` hex → replace brush resources on `Application.Current.Resources`.

- [ ] **Step 4: AppServices composition root**

```csharp
public sealed class AppServices : IDisposable
{
    public string DataDir { get; }
    public TaskStore Store { get; }
    public SettingsService Settings { get; }
    public ThemeFileService Themes { get; }
    public SchedulerSyncService SchedulerSync { get; }
    public IReminderScheduler ReminderScheduler { get; }
    public ExportImportService ExportImport { get; } = new();

    public AppServices(string? dataDir = null)
    {
        DataDir = dataDir ?? DatabasePaths.GetDefaultDataDirectory();
        Store = new TaskStore(DatabasePaths.GetDbPath(DataDir));
        Store.Initialize();
        Settings = new SettingsService(DatabasePaths.GetSettingsPath(DataDir));
        Settings.Load();
        Themes = new ThemeFileService(DatabasePaths.GetThemesPath(DataDir));
        Themes.LoadOrSeed();
        ReminderScheduler = new WindowsReminderScheduler();
        var s = Settings.Current;
        SchedulerSync = new SchedulerSyncService(ReminderScheduler, s.TaskSchedulerEnabled, s.WakeToRun);
    }

    public void Dispose() => Store.Dispose();
}
```

- [ ] **Step 5: App.xaml.cs startup**

- Parse args for `--remind {id}`
- Create mutex single-instance for UI mode
- Construct `AppServices`
- Apply theme
- If remind mode → handle notify path (Task 11) then shutdown if no main window needed
- Else show MainWindow

- [ ] **Step 6: Build + commit**

```powershell
dotnet build Focus.sln
git add src/Focus
git commit -m "feat(ui): VM base, theme resources, AppServices"
```

---

### Task 9: MainWindow chrome + MainViewModel

**Files:**
- Modify: `src/Focus/MainWindow.xaml`, `MainWindow.xaml.cs`
- Create: `src/Focus/ViewModels/MainViewModel.cs`, `TaskListItemVm.cs`, `SidebarItemVm.cs`

- [ ] **Step 1: XAML layout**

Structure:

```xml
<Window Background="{DynamicResource BgAppBrush}" Title="FOCUS" ...>
  <DockPanel>
    <!-- Toolbar Dock.Top: collapse btn, quick add TextBox, + Task, Search, Export, Settings -->
    <!-- Sidebar left: ColumnDefinition Width 200 / 48 when collapsed -->
    <!-- ListView/ItemsControl for tasks -->
  </DockPanel>
</Window>
```

Use `DynamicResource` for all colors. Sidebar sections: Folders, Views, Tags.

- [ ] **Step 2: MainViewModel behaviors**

- `Load()` folders, tags, tasks for current filter
- `SelectedFolderId` / `SelectedView` / `SelectedTagId` change → reload list
- `QuickAddText` + Enter → create task in current folder (or default)
- `ToggleSidebarCommand`
- `OpenSettingsCommand`, `OpenThemeCommand`, `NewTaskCommand`, `EditTaskCommand`, `CompleteTaskCommand`
- `ExportCommand` / `ImportCommand` using `SaveFileDialog` / `OpenFileDialog`
- After any task mutation: `Store.UpsertTask` + `SchedulerSync.SyncTask(task, exePath)` + `Refresh`

- [ ] **Step 3: Task row UI**

- Checkbox bound to complete command
- Left border color = folder color
- Title + subtitle (due, recurrence flag, tags)

- [ ] **Step 4: Run app manually**

```powershell
dotnet run --project src/Focus/Focus.csproj
```

Expected: window opens dark theme, Work/Personal visible, quick-add works.

- [ ] **Step 5: Commit**

```powershell
git add src/Focus
git commit -m "feat(ui): main window toolbar, sidebar, task list"
```

---

### Task 10: Task editor dialog

**Files:**
- Create: `src/Focus/Views/TaskEditorWindow.xaml`, `.xaml.cs`
- Create: `src/Focus/ViewModels/TaskEditorViewModel.cs`

- [ ] **Step 1: Fields**

Title, Notes, Folder ComboBox, Priority, Due DatePicker+time, Reminder DatePicker+time, Recurrence kind ComboBox, weekday CheckBoxes (Mon–Sun), TimeOfDay, IntervalN, Tags multi-select or comma add.

- [ ] **Step 2: Save**

Map VM → `TaskItem`. If recurrence set, compute `NextFireAtLocal = RecurrenceCalculator.GetNextFireLocal(rule, DateTime.Now)` (or use explicit reminder if one-shot). Persist + scheduler sync.

- [ ] **Step 3: Manual test** weekly Mon/Wed 9:00 creates task and shows in list.

- [ ] **Step 4: Commit**

```powershell
git add src/Focus
git commit -m "feat(ui): task editor with recurrence"
```

---

### Task 11: Notifications, tray, --remind path

**Files:**
- Create: `src/Focus/Services/NotificationService.cs`
- Create: `src/Focus/Services/TrayService.cs`
- Create: `src/Focus/Services/SingleInstanceService.cs`
- Modify: `App.xaml.cs`

- [ ] **Step 1: NotificationService**

```csharp
public sealed class NotificationService
{
    public void Notify(TaskItem task, AppSettings settings, Action? focusMainWindow)
    {
        if (settings.NotificationsPaused) return;
        if (settings.ToastEnabled) ShowToast(task.Title, task.Notes);
        if (settings.SoundEnabled) PlaySound(settings.SoundPath);
        if (settings.PopupFocusEnabled) focusMainWindow?.Invoke();
    }

    private void ShowToast(string title, string body)
    {
        // Microsoft.Toolkit.Uwp.Notifications
        new ToastContentBuilder()
            .AddText(title)
            .AddText(string.IsNullOrWhiteSpace(body) ? "FOCUS reminder" : body)
            .Show();
    }
}
```

Register AUMID early in App startup:

```csharp
// DesktopNotificationManagerCompat.RegisterAumidAndComServer / SetApplicationId if required
```

For unpackaged WPF, set in App constructor:

```csharp
DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("Focus.App");
DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
```

If toolkit APIs differ by package version, use the package README pattern for **desktop unpackaged** apps. Fallback: `MessageBox` only if toast registration fails (log + still try sound).

- [ ] **Step 2: TrayService**

Use `System.Windows.Forms.NotifyIcon` (enable `UseWindowsForms` in csproj) or hard-tray via WPF packages. Menu: Open, Pause notifications, Exit. Respect `CloseToTray` on MainWindow.Closing.

- [ ] **Step 3: --remind handler**

```csharp
if (args is [_, "--remind", var id] || ParseRemind(args) is string rid)
{
    using var services = new AppServices();
    var task = services.Store.GetTask(id);
    if (task is null) return;
    var notify = new NotificationService();
    // If another instance running, optionally forward; else show toast without full UI
    notify.Notify(task, services.Settings.Current, focusMainWindow: null);
    ReminderAdvance.OnFired(task, DateTime.Now);
    services.Store.UpsertTask(task);
    services.SchedulerSync.SyncTask(task, Environment.ProcessPath!);
    return; // exit after notify unless user has tray preference to keep alive
}
```

Dedupe: write `%LocalAppData%\Focus\last-fire-{id}.txt` with timestamp; ignore if same id fired &lt; 30s ago.

- [ ] **Step 4: Manual test**

1. Create reminder 2 minutes ahead.
2. Confirm Task Scheduler entry under Task Scheduler Library → Focus.
3. Close app completely.
4. Wait for fire → toast appears.
5. Confirm recurring reschedule.

- [ ] **Step 5: Commit**

```powershell
git add src/Focus
git commit -m "feat(ui): toast, tray, and --remind Task Scheduler path"
```

---

### Task 12: Settings + Theme editor windows

**Files:**
- Create: `src/Focus/Views/SettingsWindow.xaml`, `.xaml.cs`
- Create: `src/Focus/Views/ThemeEditorWindow.xaml`, `.xaml.cs`
- Create: `src/Focus/ViewModels/SettingsViewModel.cs`, `ThemeEditorViewModel.cs`

- [ ] **Step 1: Settings toggles**

Bind all `AppSettings` flags. On save: write JSON; rebuild `SchedulerSyncService` flags; resync all open tasks with reminders (`foreach` tasks with next fire → SyncTask).

- [ ] **Step 2: Theme editor**

List color fields with `TextBox` hex or simple color input; live `ThemeApplicator.Apply`; Save theme to `themes.json`; set active theme id.

- [ ] **Step 3: Folder color edit**

From sidebar context menu → small dialog name + color → `UpsertFolder`.

- [ ] **Step 4: Commit**

```powershell
git add src/Focus
git commit -m "feat(ui): settings and theme editor"
```

---

### Task 13: Polish, README, publish check

**Files:**
- Modify: `README.md`
- Modify: any rough edges (empty states, overdue brush, search filter)

- [ ] **Step 1: Empty states** — “No tasks in this view” text when list empty

- [ ] **Step 2: Search** — filter current list by title/notes substring in MainViewModel

- [ ] **Step 3: README**

Include:

- What FOCUS is
- Requirements: Windows 10/11, .NET 9 desktop runtime (or self-contained publish)
- Build: `dotnet build Focus.sln`
- Run: `dotnet run --project src/Focus`
- Publish:  
  `dotnet publish src/Focus/Focus.csproj -c Release -r win-x64 --self-contained false -o publish`
- Data folder: `%LocalAppData%\Focus\`
- Task Scheduler folder: `Focus`
- Features list matching spec
- No voice / no cloud

- [ ] **Step 4: Full test suite**

```powershell
dotnet test Focus.sln
dotnet build Focus.sln -c Release
```

Expected: all tests pass, Release build OK.

- [ ] **Step 5: Final commit**

```powershell
git add README.md src tests
git commit -m "docs: README and v1 polish for FOCUS"
```

---

## Spec coverage checklist

| Spec requirement | Task(s) |
|------------------|---------|
| C# WPF app | 1, 8–12 |
| Folders Work/Personal + custom + colors | 4, 9, 12 |
| Tags | 4, 9, 10 |
| Smart views | 4, 9 |
| One-shot + recurring (incl. weekdays+time) | 3, 7, 10 |
| Task Scheduler when app closed | 6, 11 |
| Toast / sound / focus / tray toggles | 11, 12 |
| Collapsible sidebar + toolbar | 9 |
| Focus Dark + full color map + named themes | 5, 8, 12 |
| Per-folder colors | 4, 9, 12 |
| Local SQLite + export/import | 4, 5, 9 |
| No voice | (omitted intentionally) |
| Close to tray / pause notifications | 11, 12 |
| Missed/overdue view | 4, 9 |
| Unit tests recurrence/export/theme | 3, 5, 7 |
| Single instance + --remind | 11 |
| Low compute (no Electron, timer via OS) | 6, 11 |

## Placeholder / consistency notes

- Product name **FOCUS**, assembly `Focus`, data dir `Focus`, scheduler folder `Focus`.
- Datetimes in DB: **local** wall clock strings for due/reminder/next_fire; completed/created **UTC**.
- `FocusTaskStatus` avoids clash with `System.Threading.Tasks.Task` and `System.Windows.Window` status naming.
- Target framework **net9.0-windows** (SDK present on dev machine); design doc mentioned .NET 8 — functionally equivalent for this app.

---

## Execution handoff

After this plan is accepted, implement **task-by-task** with commits as specified. Prefer running tests after each Core task before moving to UI.

---

### Task 14: Telegram + Discord messaging bridges

**Files:**
- Modify: `src/Focus.Core/Models/AppSettings.cs` � messaging fields
- Create: `src/Focus.Core/Services/Messaging/IMessageBridge.cs`
- Create: `src/Focus.Core/Services/Messaging/TodoListFormatter.cs`
- Create: `src/Focus.Core/Services/Messaging/TelegramMessageBridge.cs`
- Create: `src/Focus.Core/Services/Messaging/DiscordMessageBridge.cs`
- Create: `src/Focus.Core/Services/Messaging/MessagingService.cs`
- Create: `tests/Focus.Tests/TodoListFormatterTests.cs`
- Create: `tests/Focus.Tests/MessagingServiceTests.cs` (HttpMessageHandler fake)
- Modify: Settings UI + MainViewModel toolbar actions
- Modify: NotificationService / --remind path to call MessagingService when enabled

**Behavior:**
1. `TodoListFormatter.FormatSummary(IEnumerable<TaskItem>, folders, title)` produces plain text list
2. Telegram: POST `https://api.telegram.org/bot{token}/sendMessage` JSON chat_id + text (truncate to 4000 chars)
3. Discord: POST webhook JSON `{ "content": "..." }` (truncate to 2000 chars; split if needed)
4. Settings: toggles + token/chat/webhook fields + Test button
5. Toolbar: Send today's list
6. On reminder fire: if MessagingOnReminder, send short reminder line (+ optional today list)

**TDD:** Formatter tests; MessagingService with mocked HttpClient does not throw on 200; handles 401 gracefully.

**Commit:** `feat: optional Telegram and Discord list/reminder messaging`
