using System.Globalization;

namespace SmsTicket.Services.ActiveTickets;

/// <param name="Minutes">"{0} min"</param>
/// <param name="HoursMinutes">"{0} h {1} min"</param>
/// <param name="DaysHours">"{0} d {1} h"</param>
public sealed record RemainingTimeFormats(string Minutes, string HoursMinutes, string DaysHours);

public static class ActiveTicketFormatter
{
    public static string FormatRemaining(TimeSpan remaining, RemainingTimeFormats formats, CultureInfo culture)
    {
        // Round up: a ticket with 30 s left still shows "1 min"
        var minutes = (int)Math.Ceiling(Math.Max(0, remaining.TotalMinutes));
        return minutes switch
        {
            <= 60 => string.Format(culture, formats.Minutes, minutes),
            < 24 * 60 => string.Format(culture, formats.HoursMinutes, minutes / 60, minutes % 60),
            _ => string.Format(culture, formats.DaysHours, minutes / (24 * 60), minutes % (24 * 60) / 60),
        };
    }

    /// <summary>"14:52" today, "ne 4:00" on another day.</summary>
    public static string FormatUntil(DateTimeOffset until, DateTimeOffset now, TimeZoneInfo zone, CultureInfo culture)
    {
        var localUntil = TimeZoneInfo.ConvertTime(until, zone);
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var time = localUntil.ToString("t", culture);
        return localUntil.Date == localNow.Date ? time : $"{localUntil.ToString("ddd", culture)} {time}";
    }

    /// <summary>"21.11.2026 14:22–14:52", or both dates when the range crosses midnight.</summary>
    public static string FormatRange(DateTimeOffset from, DateTimeOffset until, TimeZoneInfo zone, CultureInfo culture)
    {
        var localFrom = TimeZoneInfo.ConvertTime(from, zone);
        var localUntil = TimeZoneInfo.ConvertTime(until, zone);
        var start = $"{localFrom.ToString("d", culture)} {localFrom.ToString("t", culture)}";
        return localFrom.Date == localUntil.Date
            ? $"{start}–{localUntil.ToString("t", culture)}"
            : $"{start} – {localUntil.ToString("d", culture)} {localUntil.ToString("t", culture)}";
    }
}
