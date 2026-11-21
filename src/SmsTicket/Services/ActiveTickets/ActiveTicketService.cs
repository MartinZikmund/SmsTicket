using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Confirmation;
using SmsTicket.Services.PurchaseHistory;

namespace SmsTicket.Services.ActiveTickets;

public sealed class ActiveTicketService
{
    private static ActiveTicketService? _default;

    private readonly IPurchaseHistoryService _history;
    private readonly TimeProvider _clock;

    public ActiveTicketService(IPurchaseHistoryService history, TimeProvider clock, TimeZoneInfo zone)
    {
        _history = history;
        _clock = clock;
        Zone = zone;
    }

    public static ActiveTicketService Default =>
        _default ??= new(PurchaseHistoryService.Default, TimeProvider.System, PragueTime.Zone);

    public IPurchaseHistoryService History => _history;

    public TimeZoneInfo Zone { get; }

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), Zone);

    public async Task<IReadOnlyList<ActiveTicket>> GetActiveAsync() =>
        ActiveTickets.Select(await _history.GetAllAsync(), Now, Zone);

    /// <summary>Overrides the validity. A null <paramref name="validUntil"/> lets the ticket's rule compute it.</summary>
    public Task<bool> SetValidityAsync(Guid id, DateTimeOffset validFrom, DateTimeOffset? validUntil, ValiditySource source) =>
        _history.UpdateAsync(id, r => r with { ValidFrom = validFrom, ValidUntil = validUntil, ValiditySource = source });

    public Task<bool> AdjustStartAsync(Guid id, DateTimeOffset validFrom) =>
        SetValidityAsync(id, validFrom, null, ValiditySource.Adjusted);

    public Task<bool> DismissAsync(Guid id) => _history.UpdateAsync(id, r => r with { DismissedAt = Now });

    /// <summary>Finds the active ticket an SMS shared into the app belongs to.</summary>
    public async Task<ConfirmationMatch> MatchConfirmationAsync(string smsText) =>
        ConfirmationMatcher.Match(await GetActiveAsync(), TicketSmsParser.Parse(smsText, Now, Zone));

    /// <summary>
    /// Stores the operator's ticket SMS with the ticket and, when its validity can be read, makes that the confirmed validity.
    /// </summary>
    public async Task<ConfirmationResult> ApplyConfirmationAsync(Guid id, string smsText)
    {
        if ((await _history.GetAllAsync()).FirstOrDefault(r => r.Id == id) is not { } record)
        {
            return new(ConfirmationOutcome.NotFound, ParsedTicketSms.Empty);
        }

        var parsed = TicketSmsParser.Parse(smsText, record.PreparedAt, Zone);
        if (!parsed.HasValidity)
        {
            var stored = await _history.UpdateAsync(id, r => r with { ConfirmationSms = smsText, TicketCode = parsed.Code ?? r.TicketCode });
            return new(stored ? ConfirmationOutcome.Unrecognized : ConfirmationOutcome.NotFound, parsed);
        }

        var rule = TicketValidityRules.Find(record.TicketId);
        var from = parsed.ValidFrom ?? EstimateStart(record, rule, parsed.ValidUntil!.Value);
        var until = parsed.ValidUntil ?? rule?.Compute(from, Zone).Until ?? from;
        if (until <= record.PreparedAt || from < record.PreparedAt - ConfirmationMatcher.ClockTolerance)
        {
            // An SMS from an earlier purchase must not overwrite this ticket
            return new(ConfirmationOutcome.Stale, parsed);
        }

        var updated = await _history.UpdateAsync(id, r => r with
        {
            ValidFrom = from,
            ValidUntil = until < from ? from : until,
            ValiditySource = ValiditySource.Confirmed,
            ConfirmationSms = smsText,
            TicketCode = parsed.Code ?? r.TicketCode,
        });
        return new(updated ? ConfirmationOutcome.Confirmed : ConfirmationOutcome.NotFound, parsed);
    }

    private static DateTimeOffset EstimateStart(PurchaseRecord record, TicketValidityRule? rule, DateTimeOffset until)
    {
        if (rule is FixedDurationRule fixedDuration)
        {
            return until - fixedDuration.Duration;
        }

        var estimate = record.ValidFrom ?? record.PreparedAt;
        return estimate < until ? estimate : until;
    }

    /// <summary>The SMS was prepared but never sent, so it was not a purchase either.</summary>
    public Task<bool> MarkNotSentAsync(Guid id) => _history.DeleteAsync(id);
}
