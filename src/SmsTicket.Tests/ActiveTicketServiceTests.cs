using System.Text.Json;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.PurchaseHistory;
using static SmsTicket.Tests.ActiveTicketsTests;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ActiveTicketServiceTests
{
    private string _directory = null!;
    private FakeTimeProvider _clock = null!;

    private string FilePath => Path.Combine(_directory, "history.json");

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _clock = new(Local(2026, 11, 21, 14, 30));
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
    public async Task GetActive_UsesClock()
    {
        var (service, history) = Create();
        await history.AddAsync(Record("PHA30min", _clock.Now.AddMinutes(-10)));

        Assert.AreEqual(1, (await service.GetActiveAsync()).Count);

        _clock.Advance(TimeSpan.FromMinutes(20));
        Assert.AreEqual(0, (await service.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task AdjustStart_PersistsAndRecomputesUntil()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", _clock.Now.AddMinutes(-10));
        await history.AddAsync(record);

        await service.AdjustStartAsync(record.Id, _clock.Now.AddMinutes(-7));

        var (reloaded, _) = Create();
        var ticket = (await reloaded.GetActiveAsync()).Single();
        Assert.AreEqual(_clock.Now.AddMinutes(-7), ticket.ValidFrom);
        Assert.AreEqual(_clock.Now.AddMinutes(23), ticket.ValidUntil);
        Assert.AreEqual(ValiditySource.Adjusted, ticket.Source);
    }

    [TestMethod]
    public async Task SetValidity_Confirmed_PersistsBothBounds()
    {
        var (service, history) = Create();
        var record = Record("PHA24h", _clock.Now.AddMinutes(-2));
        await history.AddAsync(record);
        var from = _clock.Now.AddMinutes(-1);
        var until = from.AddHours(24);

        await service.SetValidityAsync(record.Id, from, until, ValiditySource.Confirmed);

        var stored = (await new PurchaseHistoryService(FilePath).GetAllAsync()).Single();
        Assert.AreEqual(from, stored.ValidFrom);
        Assert.AreEqual(until, stored.ValidUntil);
        Assert.AreEqual(ValiditySource.Confirmed, stored.ValiditySource);
    }

    [TestMethod]
    public async Task Dismiss_HidesFromActive_KeepsHistory()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", _clock.Now);
        await history.AddAsync(record);

        await service.DismissAsync(record.Id);

        Assert.AreEqual(0, (await service.GetActiveAsync()).Count);
        Assert.AreEqual(1, (await history.GetAllAsync()).Count);
    }

    [TestMethod]
    public async Task MarkNotSent_RemovesFromHistory()
    {
        var (service, history) = Create();
        var record = Record("PHA30min", _clock.Now);
        await history.AddAsync(record);

        await service.MarkNotSentAsync(record.Id);

        Assert.AreEqual(0, (await service.GetActiveAsync()).Count);
        Assert.AreEqual(0, (await history.GetAllAsync()).Count);
    }

    [TestMethod]
    public async Task Update_UnknownId_ReturnsFalse()
    {
        var (_, history) = Create();

        Assert.IsFalse(await history.UpdateAsync(Guid.NewGuid(), r => r));
    }

    [TestMethod]
    public async Task VersionOneFile_LoadsWithoutOverrides()
    {
        // Written by the first history release
        const string json = """
            {"version":1,"purchases":[{"id":"6f9619ff-8b86-d011-b42d-00c04fc964ff","cityId":"PHA","cityName":"Praha","ticketId":"PHA30min","timeText":"30 min","price":42,"preparedAt":"2026-11-21T14:20:00+01:00"}]}
            """;
        await File.WriteAllTextAsync(FilePath, json);
        var (service, _) = Create();

        var ticket = (await service.GetActiveAsync()).Single();

        Assert.AreEqual(Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"), ticket.Record.Id);
        Assert.IsNull(ticket.Record.ValidFrom);
        Assert.IsNull(ticket.Record.ValidUntil);
        Assert.IsNull(ticket.Record.DismissedAt);
        Assert.AreEqual(ValiditySource.Estimated, ticket.Source);
        Assert.AreEqual(Local(2026, 11, 21, 14, 50), ticket.ValidUntil);
    }

    [TestMethod]
    public async Task Save_WritesCurrentVersion_AndOmitsUnsetOverrides()
    {
        var (service, history) = Create();
        var plain = Record("PHA30min", _clock.Now);
        var adjusted = Record("PHA90min", _clock.Now);
        await history.AddAsync(plain);
        await history.AddAsync(adjusted);
        await service.AdjustStartAsync(adjusted.Id, _clock.Now.AddMinutes(1));

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(FilePath));
        var root = document.RootElement;
        Assert.AreEqual(PurchaseHistoryDocument.CurrentVersion, root.GetProperty("version").GetInt32());
        var purchases = root.GetProperty("purchases").EnumerateArray().ToList();
        var plainJson = purchases.Single(p => p.GetProperty("id").GetGuid() == plain.Id);
        var adjustedJson = purchases.Single(p => p.GetProperty("id").GetGuid() == adjusted.Id);
        Assert.IsFalse(plainJson.TryGetProperty("validFrom", out _));
        Assert.IsFalse(plainJson.TryGetProperty("validitySource", out _));
        Assert.IsTrue(adjustedJson.TryGetProperty("validFrom", out _));
        Assert.AreEqual("adjusted", adjustedJson.GetProperty("validitySource").GetString());
    }

    private (ActiveTicketService Service, PurchaseHistoryService History) Create()
    {
        PurchaseHistoryService history = new(FilePath);
        return (new ActiveTicketService(history, _clock, PragueTime.Zone), history);
    }
}
