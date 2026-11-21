using SmsTicket.Data;
using SmsTicket.Data.Models;

namespace SmsTicket.Services.ActiveTickets;

/// <summary>A prepared ticket together with its (estimated or known) validity.</summary>
public sealed record ActiveTicket(
    PurchaseRecord Record,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    ValiditySource Source,
    bool RequiresConfirmation)
{
    public bool IsEstimate => Source == ValiditySource.Estimated;

    public bool HasStarted(DateTimeOffset now) => now >= ValidFrom;

    public TimeSpan Remaining(DateTimeOffset now) => now >= ValidUntil ? TimeSpan.Zero : ValidUntil - now;

    /// <summary>1 before the start, 0 once expired.</summary>
    public double RemainingFraction(DateTimeOffset now)
    {
        var total = ValidUntil - ValidFrom;
        if (now <= ValidFrom || total <= TimeSpan.Zero)
        {
            return now < ValidUntil ? 1 : 0;
        }

        return Math.Clamp((ValidUntil - now) / total, 0, 1);
    }
}

public static class ActiveTickets
{
    /// <summary>
    /// Validity starts when the operator's SMS arrives, which the app cannot see. Without a better value the
    /// start is assumed to be <see cref="PurchaseRecord.PreparedAt"/>: the real start is never earlier, so the
    /// estimated expiry (and the reminder) err on the early side - typically by the minute or so the SMS takes.
    /// </summary>
    public static ActiveTicket? Compute(PurchaseRecord record, TimeZoneInfo zone)
    {
        if (TicketValidityRules.Find(record.TicketId) is not { } rule)
        {
            return null;
        }

        var period = rule.Compute(record.ValidFrom ?? record.PreparedAt, zone);
        var source = record.ValiditySource ?? ValiditySource.Estimated;
        return new(record, period.From, record.ValidUntil ?? period.Until, source, rule.RequiresConfirmation);
    }

    /// <summary>Tickets not yet expired nor dismissed, soonest expiry first.</summary>
    public static IReadOnlyList<ActiveTicket> Select(IEnumerable<PurchaseRecord> records, DateTimeOffset now, TimeZoneInfo zone) =>
        records
            .Where(r => r.DismissedAt is null)
            .Select(r => Compute(r, zone))
            .OfType<ActiveTicket>()
            .Where(t => t.ValidUntil > now && t.ValidUntil > t.ValidFrom)
            .OrderBy(t => t.ValidUntil)
            .ThenBy(t => t.Record.PreparedAt)
            .ToList();

    /// <summary>Picks the date for a time-of-day the user entered: the occurrence nearest to when the SMS was prepared.</summary>
    public static DateTimeOffset ResolveStartTime(DateTimeOffset preparedAt, TimeSpan timeOfDay, TimeZoneInfo zone)
    {
        var preparedDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(preparedAt, zone).DateTime);
        var time = TimeOnly.FromTimeSpan(timeOfDay);

        var candidates = new[] { -1, 0, 1 }.Select(offset => PragueTime.At(preparedDate.AddDays(offset), time, zone));
        return Enumerable.MinBy(candidates, candidate => (candidate - preparedAt).Duration());
    }
}
