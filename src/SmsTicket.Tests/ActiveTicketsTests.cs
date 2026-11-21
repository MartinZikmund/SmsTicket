using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.ActiveTickets;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ActiveTicketsTests
{
    private static readonly TimeZoneInfo Prague = PragueTime.Zone;

    [TestMethod]
    public void Compute_EstimatesStartAsPreparedAt()
    {
        var prepared = Local(2026, 11, 21, 14, 22);

        var ticket = ActiveTickets.Compute(Record("PHA30min", prepared), Prague);

        Assert.IsNotNull(ticket);
        Assert.AreEqual(prepared, ticket.ValidFrom);
        Assert.AreEqual(prepared.AddMinutes(30), ticket.ValidUntil);
        Assert.AreEqual(ValiditySource.Estimated, ticket.Source);
        Assert.IsFalse(ticket.RequiresConfirmation);
    }

    [TestMethod]
    public void Compute_ValidFromOverride_RecomputesUntilFromRule()
    {
        var prepared = Local(2026, 11, 21, 14, 22);
        var record = Record("PHA30min", prepared) with
        {
            ValidFrom = prepared.AddMinutes(3),
            ValiditySource = ValiditySource.Adjusted,
        };

        var ticket = ActiveTickets.Compute(record, Prague)!;

        Assert.AreEqual(prepared.AddMinutes(3), ticket.ValidFrom);
        Assert.AreEqual(prepared.AddMinutes(33), ticket.ValidUntil);
        Assert.AreEqual(ValiditySource.Adjusted, ticket.Source);
    }

    [TestMethod]
    public void Compute_ValidUntilOverride_Wins()
    {
        var prepared = Local(2026, 11, 21, 14, 22);
        var record = Record("PHA30min", prepared) with
        {
            ValidFrom = prepared.AddMinutes(1),
            ValidUntil = prepared.AddMinutes(32),
            ValiditySource = ValiditySource.Confirmed,
        };

        var ticket = ActiveTickets.Compute(record, Prague)!;

        Assert.AreEqual(prepared.AddMinutes(32), ticket.ValidUntil);
        Assert.AreEqual(ValiditySource.Confirmed, ticket.Source);
    }

    [TestMethod]
    public void Compute_DuplicateOrUnknown_ReturnsNull()
    {
        var prepared = Local(2026, 11, 21, 14, 22);

        Assert.IsNull(ActiveTickets.Compute(Record("PHADPTA", prepared), Prague));
        Assert.IsNull(ActiveTickets.Compute(Record("NoSuchTicket", prepared), Prague));
    }

    [TestMethod]
    public void Compute_PragueDayTicket_CarriesConfirmationFlag() =>
        Assert.IsTrue(ActiveTickets.Compute(Record("PHA24h", Local(2026, 11, 21, 9, 0)), Prague)!.RequiresConfirmation);

    [TestMethod]
    public void Select_KeepsOnlyUnexpiredNonDismissed_OrderedBySoonestExpiry()
    {
        var now = Local(2026, 11, 21, 15, 0);
        var expired = Record("PHA30min", now.AddMinutes(-31));
        var expiresExactlyNow = Record("PHA30min", now.AddMinutes(-30));
        var longOne = Record("PHA90min", now.AddMinutes(-10));
        var shortOne = Record("PHA30min", now.AddMinutes(-5));
        var otherCity = Record("BR29", now.AddMinutes(-60), cityId: "BR");
        var dismissed = Record("PHA30min", now.AddMinutes(-1)) with { DismissedAt = now };
        var duplicate = Record("PHADPTA", now.AddMinutes(-1));

        var active = ActiveTickets.Select(
            [expired, expiresExactlyNow, longOne, shortOne, otherCity, dismissed, duplicate], now, Prague);

        CollectionAssert.AreEqual(
            new[] { otherCity.Id, shortOne.Id, longOne.Id },
            active.Select(t => t.Record.Id).ToArray());
    }

    [TestMethod]
    public void Select_IncludesTicketNotStartedYet()
    {
        var now = Local(2026, 11, 21, 3, 0);
        var cheb = Record("CHD", now, cityId: "CHEB");

        var active = ActiveTickets.Select([cheb], now, Prague);

        Assert.AreEqual(1, active.Count);
        Assert.IsFalse(active[0].HasStarted(now));
    }

    [TestMethod]
    public void Select_ChebAfterClosing_IsNotActive()
    {
        var now = Local(2026, 11, 21, 23, 40);

        Assert.AreEqual(0, ActiveTickets.Select([Record("CHD", now, cityId: "CHEB")], now, Prague).Count);
    }

    [TestMethod]
    public void RemainingAndProgress()
    {
        var start = Local(2026, 11, 21, 14, 0);
        var ticket = ActiveTickets.Compute(Record("PHA30min", start), Prague)!;

        Assert.AreEqual(TimeSpan.FromMinutes(20), ticket.Remaining(start.AddMinutes(10)));
        Assert.AreEqual(2.0 / 3, ticket.RemainingFraction(start.AddMinutes(10)), 1e-9);
        Assert.AreEqual(1.0, ticket.RemainingFraction(start.AddMinutes(-5)));
        Assert.AreEqual(0.0, ticket.RemainingFraction(start.AddMinutes(40)));
        Assert.AreEqual(TimeSpan.Zero, ticket.Remaining(start.AddMinutes(40)));
    }

    [TestMethod]
    public void ResolveStartTime_SameDay()
    {
        var prepared = Local(2026, 11, 21, 14, 22);

        var start = ActiveTickets.ResolveStartTime(prepared, new TimeSpan(14, 25, 0), Prague);

        Assert.AreEqual(Local(2026, 11, 21, 14, 25), start);
    }

    [TestMethod]
    public void ResolveStartTime_AfterMidnight_UsesNextDay()
    {
        var prepared = Local(2026, 11, 21, 23, 58);

        var start = ActiveTickets.ResolveStartTime(prepared, new TimeSpan(0, 2, 0), Prague);

        Assert.AreEqual(Local(2026, 11, 22, 0, 2), start);
    }

    [TestMethod]
    public void ResolveStartTime_SlightlyBeforePreparedAcrossMidnight_UsesPreviousDay()
    {
        var prepared = Local(2026, 11, 22, 0, 1);

        var start = ActiveTickets.ResolveStartTime(prepared, new TimeSpan(23, 59, 0), Prague);

        Assert.AreEqual(Local(2026, 11, 21, 23, 59), start);
    }

    internal static PurchaseRecord Record(string ticketId, DateTimeOffset preparedAt, string cityId = "PHA") => new()
    {
        Id = Guid.NewGuid(),
        CityId = cityId,
        CityName = cityId,
        TicketId = ticketId,
        TimeText = "",
        Price = 42,
        PreparedAt = preparedAt,
    };
}
