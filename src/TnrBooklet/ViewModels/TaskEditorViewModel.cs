using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Focus.Core.Models;
using Focus.Core.Recurrence;

namespace Focus.ViewModels;

public sealed class TaskEditorViewModel : ViewModelBase
{
    private string _title = "";
    private string _notes = "";
    private Folder? _selectedFolder;
    private ItemKind _kind = ItemKind.Todo;
    private TaskPriority _priority = TaskPriority.None;
    private DateTime? _dueDate;
    private string _dueTimeText = "";
    private DateTime? _reminderDate;
    private string _reminderTimeText = "";
    private RecurrenceKind _recurrenceKind = RecurrenceKind.None;
    private bool _mon, _tue, _wed, _thu, _fri, _sat, _sun;
    private string _timeOfDayText = "09:00";
    private int _intervalN = 1;
    private string _tagsText = "";

    public TaskEditorViewModel(TaskItem? existing, IReadOnlyList<Folder> folders, IReadOnlyList<Tag> tags, ItemKind defaultKind = ItemKind.Todo)
    {
        Folders = new ObservableCollection<Folder>(folders);
        AvailableTags = tags;
        IsNew = existing is null;
        Kind = existing?.Kind ?? defaultKind;

        if (existing is null)
        {
            SelectedFolder = Folders.FirstOrDefault();
            return;
        }

        SourceId = existing.Id;
        Title = existing.Title;
        Notes = existing.Notes;
        Kind = existing.Kind;
        SelectedFolder = Folders.FirstOrDefault(f => f.Id == existing.FolderId) ?? Folders.FirstOrDefault();
        Priority = existing.Priority;
        if (existing.DueAtLocal is { } due)
        {
            DueDate = due.Date;
            DueTimeText = due.ToString("HH:mm");
        }
        if (existing.ReminderAtLocal is { } rem)
        {
            ReminderDate = rem.Date;
            ReminderTimeText = rem.ToString("HH:mm");
        }
        RecurrenceKind = existing.Recurrence.Kind;
        IntervalN = Math.Max(1, existing.Recurrence.IntervalN);
        TimeOfDayText = existing.Recurrence.TimeOfDay.ToString("HH:mm");
        Mon = existing.Recurrence.IncludesWeekday(DayOfWeek.Monday);
        Tue = existing.Recurrence.IncludesWeekday(DayOfWeek.Tuesday);
        Wed = existing.Recurrence.IncludesWeekday(DayOfWeek.Wednesday);
        Thu = existing.Recurrence.IncludesWeekday(DayOfWeek.Thursday);
        Fri = existing.Recurrence.IncludesWeekday(DayOfWeek.Friday);
        Sat = existing.Recurrence.IncludesWeekday(DayOfWeek.Saturday);
        Sun = existing.Recurrence.IncludesWeekday(DayOfWeek.Sunday);

        var tagNames = tags.Where(t => existing.TagIds.Contains(t.Id)).Select(t => t.Name);
        TagsText = string.Join(", ", tagNames);
        CreatedAtUtc = existing.CreatedAtUtc;
    }

    public bool IsNew { get; }
    public string? SourceId { get; }
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
    public ObservableCollection<Folder> Folders { get; }
    public IReadOnlyList<Tag> AvailableTags { get; }
    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();
    public IReadOnlyList<RecurrenceKind> RecurrenceKinds { get; } = Enum.GetValues<RecurrenceKind>();
    public IReadOnlyList<ItemKind> Kinds { get; } = Enum.GetValues<ItemKind>();

    public string WindowTitle => IsNew
        ? (Kind == ItemKind.Note ? "New note" : "New todo")
        : (Kind == ItemKind.Note ? "Edit note" : "Edit todo");

    public bool ShowScheduleFields => Kind == ItemKind.Todo;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public Folder? SelectedFolder
    {
        get => _selectedFolder;
        set => SetProperty(ref _selectedFolder, value);
    }

    public ItemKind Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                RaisePropertyChanged(nameof(ShowScheduleFields));
                RaisePropertyChanged(nameof(WindowTitle));
                if (value == ItemKind.Note)
                {
                    DueDate = null;
                    DueTimeText = "";
                    ReminderDate = null;
                    ReminderTimeText = "";
                    RecurrenceKind = RecurrenceKind.None;
                }
            }
        }
    }

    public TaskPriority Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public string DueTimeText
    {
        get => _dueTimeText;
        set => SetProperty(ref _dueTimeText, value);
    }

    public DateTime? ReminderDate
    {
        get => _reminderDate;
        set => SetProperty(ref _reminderDate, value);
    }

    public string ReminderTimeText
    {
        get => _reminderTimeText;
        set => SetProperty(ref _reminderTimeText, value);
    }

    public RecurrenceKind RecurrenceKind
    {
        get => _recurrenceKind;
        set
        {
            if (SetProperty(ref _recurrenceKind, value))
                RaisePropertyChanged(nameof(ShowWeekdays));
        }
    }

    public bool ShowWeekdays => RecurrenceKind == RecurrenceKind.Weekly;

    public bool Mon { get => _mon; set => SetProperty(ref _mon, value); }
    public bool Tue { get => _tue; set => SetProperty(ref _tue, value); }
    public bool Wed { get => _wed; set => SetProperty(ref _wed, value); }
    public bool Thu { get => _thu; set => SetProperty(ref _thu, value); }
    public bool Fri { get => _fri; set => SetProperty(ref _fri, value); }
    public bool Sat { get => _sat; set => SetProperty(ref _sat, value); }
    public bool Sun { get => _sun; set => SetProperty(ref _sun, value); }

    public string TimeOfDayText
    {
        get => _timeOfDayText;
        set => SetProperty(ref _timeOfDayText, value);
    }

    public int IntervalN
    {
        get => _intervalN;
        set => SetProperty(ref _intervalN, Math.Max(1, value));
    }

    public string TagsText
    {
        get => _tagsText;
        set => SetProperty(ref _tagsText, value);
    }

    public string? ValidationError { get; private set; }

    public bool TryBuild(out TaskItem task)
    {
        task = null!;
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationError = "Title is required.";
            return false;
        }

        if (SelectedFolder is null)
        {
            ValidationError = "Folder is required.";
            return false;
        }

        DateTime? due = null;
        DateTime? reminder = null;
        var rule = new RecurrenceRule();

        if (Kind == ItemKind.Todo)
        {
            if (!TryCombineDue(DueDate, DueTimeText, out due, out var dueError))
            {
                ValidationError = dueError;
                return false;
            }

            if (!TryCombineReminder(ReminderDate, ReminderTimeText, out reminder, out var remError))
            {
                ValidationError = remError;
                return false;
            }

            if (!TryParseTime(TimeOfDayText, out var timeOfDay))
                timeOfDay = new TimeOnly(9, 0);

            var mask = 0;
            if (Mon) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Monday);
            if (Tue) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Tuesday);
            if (Wed) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Wednesday);
            if (Thu) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Thursday);
            if (Fri) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Friday);
            if (Sat) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Saturday);
            if (Sun) mask |= RecurrenceRule.WeekdayBit(DayOfWeek.Sunday);

            rule = new RecurrenceRule
            {
                Kind = RecurrenceKind,
                WeekdaysMask = mask,
                TimeOfDay = timeOfDay,
                IntervalN = Math.Max(1, IntervalN)
            };

            if (rule.IsRecurring)
            {
                rule.NextFireAtLocal = RecurrenceCalculator.GetNextFireLocal(rule, DateTime.Now);
                reminder ??= rule.NextFireAtLocal;
            }
        }

        var tagIds = ResolveTagIds();

        task = new TaskItem
        {
            Id = SourceId ?? Guid.NewGuid().ToString("N"),
            Title = Title.Trim(),
            Notes = Notes ?? "",
            FolderId = SelectedFolder.Id,
            Kind = Kind,
            Status = FocusTaskStatus.Open,
            Priority = Priority,
            DueAtLocal = due,
            ReminderAtLocal = reminder,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow,
            Recurrence = rule,
            TagIds = tagIds
        };
        return true;
    }

    private List<string> ResolveTagIds()
    {
        var ids = new List<string>();
        if (string.IsNullOrWhiteSpace(TagsText))
            return ids;

        foreach (var raw in TagsText.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = raw.Trim().TrimStart('#');
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var existing = AvailableTags.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                ids.Add(existing.Id);
            else
                ids.Add("__new__:" + name);
        }
        return ids;
    }

    private static bool TryCombineDue(
        DateTime? date,
        string? timeText,
        out DateTime? due,
        out string? error)
    {
        due = null;
        error = null;
        if (date is null && string.IsNullOrWhiteSpace(timeText))
            return true;
        if (date is null)
        {
            error = "Pick a due date, or clear the due time.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(timeText))
        {
            due = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local);
            return true;
        }

        if (!TryParseTime(timeText, out var t))
        {
            error = "Due time must be 24-hour, like 09:05 or 21:30.";
            return false;
        }

        due = DateTime.SpecifyKind(date.Value.Date + t.ToTimeSpan(), DateTimeKind.Local);
        return true;
    }

    private static bool TryCombineReminder(
        DateTime? date,
        string? timeText,
        out DateTime? reminder,
        out string? error)
    {
        reminder = null;
        error = null;
        var hasDate = date is not null;
        var hasTime = !string.IsNullOrWhiteSpace(timeText);
        if (!hasDate && !hasTime)
            return true;

        if (!hasTime)
        {
            error = "Enter a reminder time in 24-hour format (example: 09:05 or 21:30).";
            return false;
        }

        if (!TryParseTime(timeText, out var t))
        {
            error = "Reminder time must be 24-hour, like 09:05 or 21:30 (not 9:05 PM).";
            return false;
        }

        var day = (date ?? DateTime.Today).Date;
        reminder = DateTime.SpecifyKind(day + t.ToTimeSpan(), DateTimeKind.Local);
        if (reminder.Value < DateTime.Now.AddSeconds(-15))
        {
            error = $"Reminder {reminder.Value:t} already passed. Use a later 24-hour time or a future date.";
            reminder = null;
            return false;
        }

        return true;
    }

    private static bool TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.Trim();
        var styles = DateTimeStyles.None;
        if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, styles, out time) && !LooksLike12Hour(s))
            return true;
        if (TimeOnly.TryParse(s, CultureInfo.CurrentCulture, styles, out time) && !LooksLike12Hour(s))
            return true;

        s = s.Replace(".", ":").Replace(" ", "");
        if (Regex.IsMatch(s, @"^\d{3,4}$"))
        {
            if (s.Length == 3)
                s = "0" + s;
            return TimeOnly.TryParseExact(s, "HHmm", CultureInfo.InvariantCulture, styles, out time);
        }

        return TimeOnly.TryParseExact(
            s,
            ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"],
            CultureInfo.InvariantCulture,
            styles,
            out time);
    }

    private static bool LooksLike12Hour(string text) =>
        text.Contains("AM", StringComparison.OrdinalIgnoreCase)
        || text.Contains("PM", StringComparison.OrdinalIgnoreCase);
}
