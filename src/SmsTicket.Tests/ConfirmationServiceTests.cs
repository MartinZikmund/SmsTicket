using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Confirmation;
using SmsTicket.Services.PurchaseHistory;
using SmsTicket.Services.Reminders;
using static SmsTicket.Tests.ActiveTicketsTests;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ConfirmationServiceTests
{
    // The Tarif PID template (see TicketSmsParserTests) with this test's times, price and a placeholder code
    private const string PragueSms = """
        DP hl.m.Prahy, a.s.
        Jízdenka přestupní 30,- Kč
        Platnost:
        od 21.11.26. 14:22
        do 21.11.26. 14:52
        pásma P,0,B; mimo vlaky
        kód pro kontrolu platnosti
        AB12CD34
        """;
    private const string Garbage = "Ahoj, dorazim v 15:00, kup prosim rohliky";

    private string _directory = null!;
    private FakeTimeProvider _clock = null!;

    private string FilePath => Path.Combine(_directory, "history.json");

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _clock = new(Local(2026, 11, 21, 14, 25));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Apply_RecognizedSms_OverridesEstimate()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(record);

        var result = await service.ApplyConfirmationAsync(record.Id, PragueSms);

        Assert.AreEqual(ConfirmationOutcome.Confirmed, result.Outcome);
        var ticket = (await service.GetActiveAsync()).Single();
        Assert.AreEqual(ValiditySource.Confirmed, ticket.Source);
        Assert.AreEqual(Local(2026, 11, 21, 14, 22), ticket.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 21, 14, 52), ticket.ValidUntil);
    }

    [TestMethod]
    public async Task Apply_PersistsSmsTextAndCode()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(record);

        await service.ApplyConfirmationAsync(record.Id, PragueSms);

        var stored = (await new PurchaseHistoryService(FilePath).GetAllAsync()).Single();
        Assert.AreEqual(PragueSms, stored.ConfirmationSms);
        Assert.AreEqual("AB12CD34", stored.TicketCode);
    }

    [TestMethod]
    public async Task Apply_UnrecognizedSms_KeepsEstimate_StoresText()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(record);

        var result = await service.ApplyConfirmationAsync(record.Id, Garbage);

        Assert.AreEqual(ConfirmationOutcome.Unrecognized, result.Outcome);
        var ticket = (await service.GetActiveAsync()).Single();
        Assert.AreEqual(ValiditySource.Estimated, ticket.Source);
        Assert.AreEqual(Local(2026, 11, 21, 14, 51), ticket.ValidUntil);
        Assert.AreEqual(Garbage, ticket.Record.ConfirmationSms);
    }

    [TestMethod]
    public async Task Apply_SmsOfEarlierTicket_IsStale_AndChangesNothing()
    {
        var (service, history) = Create();
        _clock.Now = Local(2026, 11, 21, 18, 0);
        var record = Record("PHA30min", Local(2026, 11, 21, 17, 55));
        await history.AddAsync(record);

        var result = await service.ApplyConfirmationAsync(record.Id, PragueSms);

        Assert.AreEqual(ConfirmationOutcome.Stale, result.Outcome);
        var stored = (await history.GetAllAsync()).Single();
        Assert.IsNull(stored.ConfirmationSms);
        Assert.IsNull(stored.ValiditySource);
    }

    [TestMethod]
    public async Task Apply_UnknownId_NotFound()
    {
        var (service, _) = Create();

        Assert.AreEqual(ConfirmationOutcome.NotFound, (await service.ApplyConfirmationAsync(Guid.NewGuid(), PragueSms)).Outcome);
    }

    [TestMethod]
    public async Task Apply_OnlyUntil_DerivesStartFromFixedDuration()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(record);

        await service.ApplyConfirmationAsync(record.Id, "Jizdenka plati do 21.11.2026 14:52");

        var ticket = (await service.GetActiveAsync()).Single();
        Assert.AreEqual(Local(2026, 11, 21, 14, 22), ticket.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 21, 14, 52), ticket.ValidUntil);
    }

    [TestMethod]
    public async Task Apply_ConfirmedValidity_ReschedulesReminder()
    {
        var (service, history) = Create();
        FakeScheduler scheduler = new();
        ExpiryReminderCoordinator coordinator = new(history, service, scheduler, _clock, (_, _) => ("title", "body"));
        var record = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(record);
        await coordinator.SyncAsync();
        Assert.AreEqual(Local(2026, 11, 21, 14, 46), scheduler.Last.Single().NotifyAt);

        await service.ApplyConfirmationAsync(record.Id, PragueSms);
        await coordinator.SyncAsync();

        Assert.AreEqual(Local(2026, 11, 21, 14, 47), scheduler.Last.Single().NotifyAt);
    }

    [TestMethod]
    public async Task MatchShared_PicksMostRecentUnconfirmedTicketOfCity()
    {
        var (service, history) = Create();
        var olderPrague = Record("PHA30min", Local(2026, 11, 21, 14, 10));
        var newerPrague = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        var brno = Record("BR29", Local(2026, 11, 21, 14, 23), cityId: "BR");
        await history.AddAsync(olderPrague);
        await history.AddAsync(newerPrague);
        await history.AddAsync(brno);

        var match = await service.MatchConfirmationAsync(PragueSms);

        Assert.AreEqual(newerPrague.Id, match.Ticket?.Record.Id);
        CollectionAssert.AreEqual(new[] { newerPrague.Id, olderPrague.Id }, match.Candidates.Select(c => c.Record.Id).ToArray());
    }

    [TestMethod]
    public async Task MatchShared_SkipsConfirmedTickets()
    {
        var (service, history) = Create();
        var older = Record("PHA30min", Local(2026, 11, 21, 14, 10));
        var newer = Record("PHA30min", Local(2026, 11, 21, 14, 21));
        await history.AddAsync(older);
        await history.AddAsync(newer);
        await service.SetValidityAsync(newer.Id, Local(2026, 11, 21, 14, 21), null, ValiditySource.Confirmed);

        var match = await service.MatchConfirmationAsync(PragueSms);

        Assert.AreEqual(older.Id, match.Ticket?.Record.Id);
    }

    [TestMethod]
    public void Match_UnknownCity_SeveralCities_AsksUser()
    {
        var now = Local(2026, 11, 21, 14, 25);
        var tickets = new[]
        {
            ActiveTickets.Compute(Record("PHA30min", now.AddMinutes(-3)), PragueTime.Zone)!,
            ActiveTickets.Compute(Record("BR29", now.AddMinutes(-2), cityId: "BR"), PragueTime.Zone)!,
        };

        var match = ConfirmationMatcher.Match(tickets, new(null, now.AddMinutes(30), null, null, null));

        Assert.IsNull(match.Ticket);
        Assert.AreEqual(2, match.Candidates.Count);
    }

    [TestMethod]
    public void Match_UnknownCity_SingleCity_PicksMostRecent()
    {
        var now = Local(2026, 11, 21, 14, 25);
        var older = ActiveTickets.Compute(Record("PHA90min", now.AddMinutes(-20)), PragueTime.Zone)!;
        var newer = ActiveTickets.Compute(Record("PHA30min", now.AddMinutes(-3)), PragueTime.Zone)!;

        var match = ConfirmationMatcher.Match([older, newer], ParsedTicketSms.Empty);

        Assert.AreSame(newer, match.Ticket);
    }

    [TestMethod]
    public void Match_PrefersTicketWithMatchingPrice()
    {
        var now = Local(2026, 11, 21, 14, 25);
        var cheap = ActiveTickets.Compute(Record("PHA30min", now.AddMinutes(-5)) with { Price = 30 }, PragueTime.Zone)!;
        var pricey = ActiveTickets.Compute(Record("PHA90min", now.AddMinutes(-3)) with { Price = 50 }, PragueTime.Zone)!;

        var match = ConfirmationMatcher.Match([cheap, pricey], new(null, null, 30, null, "PHA"));

        Assert.AreSame(cheap, match.Ticket);
    }

    [TestMethod]
    public void Match_IgnoresTicketsPreparedAfterValidityStarted()
    {
        var now = Local(2026, 11, 21, 14, 50);
        var earlier = ActiveTickets.Compute(Record("PHA30min", Local(2026, 11, 21, 14, 21)), PragueTime.Zone)!;
        var later = ActiveTickets.Compute(Record("PHA30min", Local(2026, 11, 21, 14, 45)), PragueTime.Zone)!;

        var match = ConfirmationMatcher.Match([earlier, later], new(Local(2026, 11, 21, 14, 22), null, null, null, "PHA"));

        Assert.AreSame(earlier, match.Ticket);
    }

    [TestMethod]
    public void Match_OtherCityOnly_NoMatch()
    {
        var now = Local(2026, 11, 21, 14, 25);
        var brno = ActiveTickets.Compute(Record("BR29", now.AddMinutes(-2), cityId: "BR"), PragueTime.Zone)!;

        var match = ConfirmationMatcher.Match([brno], new(null, null, null, null, "PHA"));

        Assert.IsNull(match.Ticket);
        Assert.AreEqual(0, match.Candidates.Count);
    }

    private (ActiveTicketService Service, PurchaseHistoryService History) Create()
    {
        PurchaseHistoryService history = new(FilePath);
        return (new ActiveTicketService(history, _clock, PragueTime.Zone), history);
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
