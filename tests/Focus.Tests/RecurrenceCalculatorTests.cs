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
        var rule = new RecurrenceRule { Kind = RecurrenceKind.Daily, TimeOfDay = new TimeOnly(9, 0) };
        var from = new DateTime(2026, 7, 10, 10, 0, 0);
        RecurrenceCalculator.GetNextFireLocal(rule, from).Should().Be(new DateTime(2026, 7, 11, 9, 0, 0));
    }

    [Fact]
    public void Weekly_mon_wed_skips_to_wednesday()
    {
        var rule = new RecurrenceRule
        {
            Kind = RecurrenceKind.Weekly,
            TimeOfDay = new TimeOnly(9, 0),
            WeekdaysMask = RecurrenceRule.WeekdayBit(DayOfWeek.Monday) | RecurrenceRule.WeekdayBit(DayOfWeek.Wednesday)
        };
        var from = new DateTime(2026, 7, 7, 8, 0, 0); // Tuesday
        RecurrenceCalculator.GetNextFireLocal(rule, from).Should().Be(new DateTime(2026, 7, 8, 9, 0, 0));
    }

    [Fact]
    public void Every_n_days_adds_interval()
    {
        var rule = new RecurrenceRule { Kind = RecurrenceKind.EveryNDays, IntervalN = 3, TimeOfDay = new TimeOnly(14, 30) };
        var from = new DateTime(2026, 7, 10, 15, 0, 0);
        RecurrenceCalculator.GetNextFireLocal(rule, from).Should().Be(new DateTime(2026, 7, 13, 14, 30, 0));
    }

    [Fact]
    public void Monthly_same_day_next_month()
    {
        var rule = new RecurrenceRule { Kind = RecurrenceKind.Monthly, TimeOfDay = new TimeOnly(8, 0) };
        var from = new DateTime(2026, 1, 31, 9, 0, 0);
        RecurrenceCalculator.GetNextFireLocal(rule, from).Should().Be(new DateTime(2026, 2, 28, 8, 0, 0));
    }

    [Fact]
    public void None_returns_null()
    {
        var rule = new RecurrenceRule { Kind = RecurrenceKind.None };
        RecurrenceCalculator.GetNextFireLocal(rule, DateTime.Now).Should().BeNull();
    }
}
