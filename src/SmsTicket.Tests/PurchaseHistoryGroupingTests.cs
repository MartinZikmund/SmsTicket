using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.PurchaseHistory;

namespace SmsTicket.Tests;

[TestClass]
public class PurchaseHistoryGroupingTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [TestMethod]
    public void Empty_ReturnsNoGroups() =>
        Assert.AreEqual(0, PurchaseHistoryGrouping.GroupByMonth([], Utc).Count);

    [TestMethod]
    public void GroupsByMonth_NewestMonthFirst_WithTotals()
    {
        var records = new[]
        {
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 10, 3), price: 42),
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 11, 20), price: 55),
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 11, 1), price: 42),
            PurchaseHistoryServiceTests.CreateRecord(At(2025, 11, 15), price: 30),
        };

        var groups = PurchaseHistoryGrouping.GroupByMonth(records, Utc);

        CollectionAssert.AreEqual(
            new[] { (2026, 11), (2026, 10), (2025, 11) },
            groups.Select(g => (g.Year, g.Month)).ToArray());
        CollectionAssert.AreEqual(new[] { 97, 42, 30 }, groups.Select(g => g.TotalPrice).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 1, 1 }, groups.Select(g => g.Items.Count).ToArray());
    }

    [TestMethod]
    public void ItemsWithinMonth_AreNewestFirst()
    {
        var records = new[]
        {
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 11, 1)),
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 11, 20)),
            PurchaseHistoryServiceTests.CreateRecord(At(2026, 11, 10)),
        };

        var group = PurchaseHistoryGrouping.GroupByMonth(records, Utc).Single();

        CollectionAssert.AreEqual(
            new[] { At(2026, 11, 20), At(2026, 11, 10), At(2026, 11, 1) },
            group.Items.Select(r => r.PreparedAt).ToArray());
    }

    [TestMethod]
    public void MonthBoundary_UsesTheGivenTimeZone()
    {
        // 23:30 UTC on Oct 31 is already November in Prague
        var record = PurchaseHistoryServiceTests.CreateRecord(new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero));
        var prague = TimeZoneInfo.CreateCustomTimeZone("TestCET", TimeSpan.FromHours(1), "CET", "CET");

        var utcGroup = PurchaseHistoryGrouping.GroupByMonth([record], Utc).Single();
        var pragueGroup = PurchaseHistoryGrouping.GroupByMonth([record], prague).Single();

        Assert.AreEqual(10, utcGroup.Month);
        Assert.AreEqual(11, pragueGroup.Month);
    }

    [TestMethod]
    public void FromTicket_CopiesCityAndTicketData()
    {
        var city = DataSource.Cities.First(c => c.Id == "PHA");
        var ticket = city.TicketTypes.First(t => t.Id == "PHA90min");
        var now = new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.FromHours(1));

        var record = PurchaseRecord.FromTicket(city, ticket, now);

        Assert.AreNotEqual(Guid.Empty, record.Id);
        Assert.AreEqual("PHA", record.CityId);
        Assert.AreEqual(city.Name, record.CityName);
        Assert.AreEqual("PHA90min", record.TicketId);
        Assert.AreEqual("90 min", record.TimeText);
        Assert.AreEqual(55, record.Price);
        Assert.AreEqual(now, record.PreparedAt);
    }

    [TestMethod]
    public void FindCityForTicket_ReturnsOwningCity()
    {
        var city = DataSource.FindCityForTicket("LIB45");

        Assert.IsNotNull(city);
        Assert.AreEqual("LBC", city.Id);
        Assert.IsNull(DataSource.FindCityForTicket("nope"));
    }

    private static DateTimeOffset At(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);
}
