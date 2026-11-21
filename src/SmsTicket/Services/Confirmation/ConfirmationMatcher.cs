using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;

namespace SmsTicket.Services.Confirmation;

/// <param name="Ticket">The ticket the SMS belongs to, or null when the user has to pick one of <paramref name="Candidates"/>.</param>
/// <param name="Candidates">Plausible tickets, most recently prepared first.</param>
public sealed record ConfirmationMatch(ActiveTicket? Ticket, IReadOnlyList<ActiveTicket> Candidates);

/// <summary>Finds the active ticket a shared ticket SMS belongs to.</summary>
public static class ConfirmationMatcher
{
    // Phone and operator clocks may disagree a little
    internal static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(10);

    public static ConfirmationMatch Match(IEnumerable<ActiveTicket> active, ParsedTicketSms parsed)
    {
        var candidates = active.Where(t => t.Source != ValiditySource.Confirmed).ToList();
        if (parsed.CityId is { } cityId)
        {
            candidates = candidates.Where(t => t.Record.CityId == cityId).ToList();
        }

        // The remaining filters only narrow down; a mismatch there may just be a misread SMS
        candidates = Prefer(candidates, t => parsed.Price is { } price && t.Record.Price == price);
        candidates = Prefer(candidates, t => parsed.ValidFrom is { } from && t.Record.PreparedAt <= from + ClockTolerance);

        candidates = candidates.OrderByDescending(t => t.Record.PreparedAt).ToList();
        if (candidates.Count == 0)
        {
            return new(null, candidates);
        }

        var isUnambiguous = parsed.CityId is not null || candidates.Select(t => t.Record.CityId).Distinct().Count() == 1;
        return new(isUnambiguous ? candidates[0] : null, candidates);
    }

    private static List<ActiveTicket> Prefer(List<ActiveTicket> candidates, Func<ActiveTicket, bool> predicate)
    {
        var preferred = candidates.Where(predicate).ToList();
        return preferred.Count > 0 ? preferred : candidates;
    }
}
