using SmsTicket.Data;
using SmsTicket.Data.Models;
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

    /// <summary>The SMS was prepared but never sent, so it was not a purchase either.</summary>
    public Task<bool> MarkNotSentAsync(Guid id) => _history.DeleteAsync(id);
}
