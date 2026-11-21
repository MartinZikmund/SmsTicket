namespace SmsTicket.Data;

public readonly record struct ValidityPeriod(DateTimeOffset From, DateTimeOffset Until);

/// <summary>How long a ticket is valid once the operator's confirmation SMS arrives.</summary>
public abstract record TicketValidityRule
{
    /// <summary>The operator asks for a reply ("Ano") before the ticket is issued.</summary>
    public bool RequiresConfirmation { get; init; }

    public abstract ValidityPeriod Compute(DateTimeOffset start, TimeZoneInfo zone);
}

/// <summary>Elapsed time, so a 24 h ticket lasts 24 real hours even across a DST change.</summary>
public sealed record FixedDurationRule(TimeSpan Duration) : TicketValidityRule
{
    public override ValidityPeriod Compute(DateTimeOffset start, TimeZoneInfo zone) => new(start, start + Duration);
}

/// <summary>Duration depends on whether the start falls on a Czech working day.</summary>
public sealed record WorkingDayDurationRule(TimeSpan WorkingDay, TimeSpan OtherDay) : TicketValidityRule
{
    public override ValidityPeriod Compute(DateTimeOffset start, TimeZoneInfo zone)
    {
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
        return new(start, start + (CzechPublicHolidays.IsWorkingDay(date) ? WorkingDay : OtherDay));
    }
}

/// <summary>Valid until a wall-clock time on the day after the start.</summary>
public sealed record UntilNextDayRule(TimeOnly Time) : TicketValidityRule
{
    public override ValidityPeriod Compute(DateTimeOffset start, TimeZoneInfo zone)
    {
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
        return new(start, PragueTime.At(date.AddDays(1), Time, zone));
    }
}

/// <summary>Valid within a daily window on the day of the start; before the window opens validity starts at opening.</summary>
public sealed record DailyWindowRule(TimeOnly Opens, TimeOnly Closes) : TicketValidityRule
{
    public override ValidityPeriod Compute(DateTimeOffset start, TimeZoneInfo zone)
    {
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
        var opens = PragueTime.At(date, Opens, zone);
        var closes = PragueTime.At(date, Closes, zone);
        var from = start < opens ? opens : start;
        return new(from, closes < from ? from : closes);
    }
}

/// <summary>Validity rules keyed by ticket id. Duplicates have none - they re-send an existing ticket.</summary>
public static class TicketValidityRules
{
    private static readonly Dictionary<string, TicketValidityRule> _rules = Build();

    public static IEnumerable<string> TicketIds => _rules.Keys;

    public static TicketValidityRule? Find(string ticketId) => _rules.GetValueOrDefault(ticketId);

    private static Dictionary<string, TicketValidityRule> Build()
    {
        Dictionary<string, TicketValidityRule> rules = new();

        void Minutes(int minutes, params string[] ids) => Add(new FixedDurationRule(TimeSpan.FromMinutes(minutes)), ids);
        void Hours(int hours, params string[] ids) => Add(new FixedDurationRule(TimeSpan.FromHours(hours)), ids);
        void Add(TicketValidityRule rule, params string[] ids)
        {
            foreach (var id in ids)
            {
                rules.Add(id, rule);
            }
        }

        Minutes(20, "BR20");
        Minutes(30, "PHA30min");
        Minutes(35, "PMDP35M");
        Minutes(60, "LIB", "BUD25", "HK", "HKZ", "MDJ", "COM", "COMZ");
        Minutes(70, "DSZO", "DSZOZ");
        Minutes(75, "BR29");
        Minutes(90, "PHA90min", "LIB45");
        Hours(24, "BUD70", "HK24", "DPO24", "DPO24Z", "DPMP24", "PMDP24H", "DSZO24", "DSZO24Z", "BR99");

        Add(new FixedDurationRule(TimeSpan.FromHours(24)) { RequiresConfirmation = true }, "PHA24h");
        Add(new FixedDurationRule(TimeSpan.FromHours(72)) { RequiresConfirmation = true }, "PHA72h");

        Add(new WorkingDayDurationRule(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(70)), "DPMO");
        Add(new WorkingDayDurationRule(TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(60)), "DPMP");
        Add(new WorkingDayDurationRule(TimeSpan.FromMinutes(70), TimeSpan.FromMinutes(90)), "DPO70", "DPO70Z");

        Add(new UntilNextDayRule(new TimeOnly(4, 0)), "MDJ106", "MDJZD", "MDJ60");
        Add(new DailyWindowRule(new TimeOnly(4, 30), new TimeOnly(23, 30)), "CHD");

        return rules;
    }
}
