using SmsTicket.Services.ActiveTickets;

namespace SmsTicket.Services.Reminders;

public sealed record ExpiryReminder(Guid TicketId, DateTimeOffset NotifyAt, string Title, string Body)
{
    /// <summary>Stable per ticket, so rescheduling replaces the earlier notification.</summary>
    public string Tag => TicketId.ToString("N");
}

public static class ExpiryReminderPlanner
{
    public static readonly TimeSpan DefaultLeadTime = TimeSpan.FromMinutes(5);

    /// <summary>When to notify, or null when it is already too late for a useful heads-up.</summary>
    public static DateTimeOffset? GetReminderTime(ActiveTicket ticket, DateTimeOffset now, TimeSpan leadTime)
    {
        var notifyAt = ticket.ValidUntil - leadTime;
        return notifyAt > now ? notifyAt : null;
    }

    /// <summary>
    /// A window for an inexact alarm that ends exactly at <paramref name="target"/>: firing a little early is fine,
    /// firing late would defeat the reminder.
    /// </summary>
    public static (DateTimeOffset Start, TimeSpan Length) GetInexactWindow(DateTimeOffset target, DateTimeOffset now, TimeSpan maxLength)
    {
        var start = target - maxLength;
        if (start < now)
        {
            start = now;
        }

        return (start, target - start);
    }
}
