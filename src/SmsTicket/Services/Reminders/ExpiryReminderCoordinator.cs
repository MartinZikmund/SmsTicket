using System.Diagnostics;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.PurchaseHistory;

namespace SmsTicket.Services.Reminders;

/// <summary>Keeps scheduled expiry reminders in line with the active tickets.</summary>
public sealed class ExpiryReminderCoordinator
{
    private readonly IPurchaseHistoryService _history;
    private readonly ActiveTicketService _activeTickets;
    private readonly IExpiryReminderScheduler _scheduler;
    private readonly TimeProvider _clock;
    private readonly Func<ActiveTicket, DateTimeOffset, (string Title, string Body)> _texts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ExpiryReminderCoordinator(
        IPurchaseHistoryService history,
        ActiveTicketService activeTickets,
        IExpiryReminderScheduler scheduler,
        TimeProvider clock,
        Func<ActiveTicket, DateTimeOffset, (string Title, string Body)> texts)
    {
        _history = history;
        _activeTickets = activeTickets;
        _scheduler = scheduler;
        _clock = clock;
        _texts = texts;
    }

    public TimeSpan LeadTime { get; init; } = ExpiryReminderPlanner.DefaultLeadTime;

    /// <summary>Syncs now and after every history change.</summary>
    public void Start()
    {
        _history.Changed += (_, _) => _ = SyncSafeAsync();
        _ = SyncSafeAsync();
    }

    public async Task SyncAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var now = _clock.GetUtcNow();
            List<ExpiryReminder> reminders = new();
            foreach (var ticket in await _activeTickets.GetActiveAsync())
            {
                if (ExpiryReminderPlanner.GetReminderTime(ticket, now, LeadTime) is { } notifyAt)
                {
                    var (title, body) = _texts(ticket, now);
                    reminders.Add(new(ticket.Record.Id, notifyAt, title, body));
                }
            }

            await _scheduler.ReplaceAllAsync(reminders);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SyncSafeAsync()
    {
        try
        {
            await SyncAsync();
        }
        catch (Exception ex)
        {
            // A reminder is a nicety; never crash the app over it
            Trace.WriteLine($"Expiry reminders could not be scheduled: {ex}");
        }
    }
}
