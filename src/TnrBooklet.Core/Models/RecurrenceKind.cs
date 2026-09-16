namespace Focus.Core.Models;

public enum RecurrenceKind
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    EveryNDays = 4,
    /// <summary>Every N hours. IntervalN is hours (1 = hourly).</summary>
    Hourly = 5
}
