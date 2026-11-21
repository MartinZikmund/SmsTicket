namespace SmsTicket.Data;

/// <summary>Ticket validity follows Czech local time regardless of the device's time zone.</summary>
public static class PragueTime
{
    public static TimeZoneInfo Zone { get; } = FindZone();

    /// <summary>Converts a wall-clock time in <paramref name="zone"/> to an instant. Times skipped by DST move forward.</summary>
    public static DateTimeOffset At(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Europe/Prague", "Central Europe Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.Local;
    }
}
