using SmsTicket.Data;
using SmsTicket.Services.ActiveTickets;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.PurchaseHistory;
using SmsTicket.ViewModels;
using static SmsTicket.Tests.ActiveTicketsTests;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ActiveTicketsViewModelTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Setup() => _directory = Path.Combine(Path.GetTempPath(), "SmsTicketTests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Refresh_UpdatesItemsInPlace_AndDropsExpired()
    {
        FakeTimeProvider clock = new(Local(2026, 11, 21, 14, 0));
        PurchaseHistoryService history = new(Path.Combine(_directory, "history.json"));
        ActiveTicketsViewModel viewModel = new(new ActiveTicketService(history, clock, PragueTime.Zone), new LocalizationService());
        var shortOne = Record("PHA30min", clock.Now);
        var longOne = Record("PHA90min", clock.Now);
        await history.AddAsync(longOne);
        await history.AddAsync(shortOne);

        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.HasItems);
        CollectionAssert.AreEqual(new[] { shortOne.Id, longOne.Id }, viewModel.Items.Select(i => i.Id).ToArray());
        var longItem = viewModel.Items[1];
        Assert.AreEqual(100, longItem.Progress, 0.01);

        clock.Advance(TimeSpan.FromMinutes(45));
        viewModel.Refresh();

        Assert.AreEqual(1, viewModel.Items.Count);
        Assert.AreSame(longItem, viewModel.Items[0]);
        Assert.AreEqual(50, longItem.Progress, 0.01);

        clock.Advance(TimeSpan.FromMinutes(45));
        viewModel.Refresh();

        Assert.IsFalse(viewModel.HasItems);
    }

    [TestMethod]
    public async Task Refresh_RaisesHasItemsOnlyWhenItChanges()
    {
        FakeTimeProvider clock = new(Local(2026, 11, 21, 14, 0));
        PurchaseHistoryService history = new(Path.Combine(_directory, "history.json"));
        ActiveTicketsViewModel viewModel = new(new ActiveTicketService(history, clock, PragueTime.Zone), new LocalizationService());
        await history.AddAsync(Record("PHA30min", clock.Now));
        var raised = 0;
        viewModel.PropertyChanged += (_, e) => raised += e.PropertyName == nameof(ActiveTicketsViewModel.HasItems) ? 1 : 0;

        await viewModel.LoadAsync();
        viewModel.Refresh();
        viewModel.Refresh();

        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public async Task AdjustStart_ResolvesTimeOfDayAgainstPreparedAt()
    {
        FakeTimeProvider clock = new(Local(2026, 11, 21, 23, 58));
        PurchaseHistoryService history = new(Path.Combine(_directory, "history.json"));
        ActiveTicketsViewModel viewModel = new(new ActiveTicketService(history, clock, PragueTime.Zone), new LocalizationService());
        await history.AddAsync(Record("PHA30min", clock.Now));
        await viewModel.LoadAsync();

        await viewModel.AdjustStartAsync(viewModel.Items[0], new TimeSpan(0, 1, 0));
        await viewModel.LoadAsync();

        var item = viewModel.Items.Single();
        Assert.AreEqual(Local(2026, 11, 22, 0, 31), item.Ticket.ValidUntil);
        Assert.IsFalse(item.IsEstimate);
    }
}
