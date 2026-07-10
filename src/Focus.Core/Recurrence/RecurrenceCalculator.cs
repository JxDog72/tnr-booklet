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
