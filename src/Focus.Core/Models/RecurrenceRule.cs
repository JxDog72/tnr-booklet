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
