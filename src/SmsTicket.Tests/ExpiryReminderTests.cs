using SmsTicket.Data;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.PurchaseHistory;
using SmsTicket.Services.Reminders;
using static SmsTicket.Tests.ActiveTicketsTests;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ExpiryReminderTests
{
    private static readonly TimeSpan Lead = ExpiryReminderPlanner.DefaultLeadTime;

    [TestMethod]
    public void DefaultLead_IsFiveMinutes() => Assert.AreEqual(TimeSpan.FromMinutes(5), Lead);

    [TestMethod]
    public void ReminderTime_IsLeadBeforeExpiry()
    {
        var now = Local(2026, 11, 21, 14, 0);
        var ticket = ActiveTickets.Compute(Record("PHA30min", now), PragueTime.Zone)!;

        Assert.AreEqual(now.AddMinutes(25), ExpiryReminderPlanner.GetReminderTime(ticket, now, Lead));
    }

    [TestMethod]
    public void ReminderTime_TooLate_ReturnsNull()
    {
        var start = Local(2026, 11, 21, 14, 0);
        var ticket = ActiveTickets.Compute(Record("PHA30min", start), PragueTime.Zone)!;

        Assert.IsNull(ExpiryReminderPlanner.GetReminderTime(ticket, start.AddMinutes(26), Lead));
        Assert.IsNull(ExpiryReminderPlanner.GetReminderTime(ticket, start.AddMinutes(25), Lead));
        Assert.IsNotNull(ExpiryReminderPlanner.GetReminderTime(ticket, start.AddMinutes(24), Lead));
    }

    [TestMethod]
    public void InexactWindow_EndsAtTarget()
    {
        var now = Local(2026, 11, 21, 14, 0);
        var target = now.AddMinutes(25);

        var (start, length) = ExpiryReminderPlanner.GetInexactWindow(target, now, TimeSpan.FromMinutes(5));

        Assert.AreEqual(target.AddMinutes(-5), start);
        Assert.AreEqual(target, start + length);
    }

    [TestMethod]
    public void InexactWindow_NeverStartsInThePast()
    {
        var now = Local(2026, 11, 21, 14, 0);
        var target = now.AddMinutes(2);

        var (start, length) = ExpiryReminderPlanner.GetInexactWindow(target, now, TimeSpan.FromMinutes(5));

        Assert.AreEqual(now, start);
        Assert.AreEqual(target, start + length);
    }

    [TestMethod]
    public async Task Coordinator_SchedulesActiveTickets_AndUpdatesOnChange()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));
        try
        {
            FakeTimeProvider clock = new(Local(2026, 11, 21, 14, 0));
            PurchaseHistoryService history = new(Path.Combine(directory, "history.json"));
            ActiveTicketService active = new(history, clock, PragueTime.Zone);
            FakeScheduler scheduler = new();
            ExpiryReminderCoordinator coordinator = new(history, active, scheduler, clock, (_, _) => ("title", "body"));

            var first = Record("PHA30min", clock.Now);
            var second = Record("BR29", clock.Now.AddMinutes(-1), cityId: "BR");
            var almostExpired = Record("PHA30min", clock.Now.AddMinutes(-27));
            await history.AddAsync(first);
            await history.AddAsync(second);
            await history.AddAsync(almostExpired);
            await coordinator.SyncAsync();

            CollectionAssert.AreEquivalent(
                new[] { (first.Id, clock.Now.AddMinutes(25)), (second.Id, clock.Now.AddMinutes(69)) },
                scheduler.Last.Select(r => (r.TicketId, r.NotifyAt)).ToArray());

            await active.AdjustStartAsync(first.Id, clock.Now.AddMinutes(2));
            await coordinator.SyncAsync();
            Assert.AreEqual(clock.Now.AddMinutes(27), scheduler.Last.Single(r => r.TicketId == first.Id).NotifyAt);

            await active.MarkNotSentAsync(first.Id);
            await active.DismissAsync(second.Id);
            await coordinator.SyncAsync();
            Assert.AreEqual(0, scheduler.Last.Count);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class FakeScheduler : IExpiryReminderScheduler
    {
        public IReadOnlyList<ExpiryReminder> Last { get; private set; } = [];

        public Task ReplaceAllAsync(IReadOnlyList<ExpiryReminder> reminders)
        {
            Last = reminders;
            return Task.CompletedTask;
        }
    }
}
