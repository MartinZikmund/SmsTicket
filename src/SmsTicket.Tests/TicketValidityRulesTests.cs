using System.Text.RegularExpressions;
using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public partial class TicketValidityRulesTests
{
    private static readonly TimeZoneInfo Prague = PragueTime.Zone;

    [TestMethod]
    public void EveryNonDuplicateTicket_HasRule()
    {
        var missing = DataSource.Cities
            .SelectMany(c => c.TicketTypes)
            .Where(t => !t.IsDuplicate && TicketValidityRules.Find(t.Id) is null)
            .Select(t => t.Id)
            .ToList();

        Assert.AreEqual(0, missing.Count, $"Tickets without a validity rule: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void Duplicates_HaveNoRule()
    {
        foreach (var ticket in DataSource.Cities.SelectMany(c => c.TicketTypes).Where(t => t.IsDuplicate))
        {
            Assert.IsNull(TicketValidityRules.Find(ticket.Id), ticket.Id);
        }
    }

    [TestMethod]
    public void Rules_OnlyReferenceExistingTickets()
    {
        var ids = DataSource.Cities.SelectMany(c => c.TicketTypes).Select(t => t.Id).ToHashSet();

        var stale = TicketValidityRules.TicketIds.Where(id => !ids.Contains(id)).ToList();

        Assert.AreEqual(0, stale.Count, $"Rules for unknown tickets: {string.Join(", ", stale)}");
    }

    [TestMethod]
    public void DurationRules_MatchDisplayedTimeText()
    {
        foreach (var ticket in DataSource.Cities.SelectMany(c => c.TicketTypes))
        {
            var duration = TicketValidityRules.Find(ticket.Id) switch
            {
                FixedDurationRule fixedRule => fixedRule.Duration,
                WorkingDayDurationRule workday => workday.WorkingDay,
                _ => (TimeSpan?)null,
            };
            if (duration is null)
            {
                continue;
            }

            var match = TimeTextRegex().Match(ticket.TimeText);
            Assert.IsTrue(match.Success, $"{ticket.Id}: unexpected time text '{ticket.TimeText}'");
            var value = int.Parse(match.Groups[1].Value);
            var expected = match.Groups[2].Value == "h" ? TimeSpan.FromHours(value) : TimeSpan.FromMinutes(value);
            Assert.AreEqual(expected, duration, ticket.Id);
        }
    }

    [TestMethod]
    [DataRow("PHA30min", 30)]
    [DataRow("PHA90min", 90)]
    [DataRow("BR20", 20)]
    [DataRow("BR29", 75)]
    [DataRow("PMDP35M", 35)]
    [DataRow("LIB", 60)]
    [DataRow("LIB45", 90)]
    [DataRow("DSZO", 70)]
    [DataRow("COM", 60)]
    [DataRow("HK24", 24 * 60)]
    [DataRow("PHA72h", 72 * 60)]
    public void FixedDuration(string ticketId, int minutes)
    {
        var start = Local(2026, 11, 21, 10, 0);

        var period = Compute(ticketId, start);

        Assert.AreEqual(start, period.From);
        Assert.AreEqual(start.AddMinutes(minutes), period.Until);
    }

    [TestMethod]
    [DataRow("DPMO", 50, 70)]
    [DataRow("DPMP", 45, 60)]
    [DataRow("DPO70", 70, 90)]
    [DataRow("DPO70Z", 70, 90)]
    public void WorkingDayDuration(string ticketId, int workingDay, int otherDay)
    {
        var friday = Local(2026, 11, 20, 12, 0);
        var saturday = Local(2026, 11, 21, 12, 0);
        var sunday = Local(2026, 11, 22, 12, 0);
        var holiday = Local(2026, 11, 17, 12, 0);
        var goodFriday = Local(2026, 4, 3, 12, 0);
        var easterMonday = Local(2026, 4, 6, 12, 0);

        Assert.AreEqual(friday.AddMinutes(workingDay), Compute(ticketId, friday).Until);
        Assert.AreEqual(saturday.AddMinutes(otherDay), Compute(ticketId, saturday).Until);
        Assert.AreEqual(sunday.AddMinutes(otherDay), Compute(ticketId, sunday).Until);
        Assert.AreEqual(holiday.AddMinutes(otherDay), Compute(ticketId, holiday).Until);
        Assert.AreEqual(goodFriday.AddMinutes(otherDay), Compute(ticketId, goodFriday).Until);
        Assert.AreEqual(easterMonday.AddMinutes(otherDay), Compute(ticketId, easterMonday).Until);
    }

    [TestMethod]
    public void WorkingDayDuration_UsesLocalDateOfStart_AtMidnight()
    {
        // Friday 23:59 is a working day, Saturday 00:00 is not
        var fridayLate = Local(2026, 11, 20, 23, 59);
        var saturdayMidnight = Local(2026, 11, 21, 0, 0);

        Assert.AreEqual(fridayLate.AddMinutes(50), Compute("DPMO", fridayLate).Until);
        Assert.AreEqual(saturdayMidnight.AddMinutes(70), Compute("DPMO", saturdayMidnight).Until);
    }

    [TestMethod]
    public void WorkingDayDuration_UsesPragueDate_NotUtcDate()
    {
        // 23:30 UTC on Friday is already Saturday 00:30 in Prague
        var start = new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero);

        Assert.AreEqual(start.AddMinutes(70), Compute("DPMO", start).Until);
    }

    [TestMethod]
    [DataRow("MDJ106")]
    [DataRow("MDJZD")]
    [DataRow("MDJ60")]
    public void UntilFourNextDay(string ticketId)
    {
        var start = Local(2026, 11, 21, 9, 15);

        var period = Compute(ticketId, start);

        Assert.AreEqual(start, period.From);
        Assert.AreEqual(Local(2026, 11, 22, 4, 0), period.Until);
    }

    [TestMethod]
    public void UntilFourNextDay_BoughtAfterMidnight_ValidUntilFollowingMorning()
    {
        var start = Local(2026, 11, 22, 0, 30);

        Assert.AreEqual(Local(2026, 11, 23, 4, 0), Compute("MDJ106", start).Until);
    }

    [TestMethod]
    public void UntilFourNextDay_AcrossSpringDstChange()
    {
        // 29 March 2026: clocks go 02:00 -> 03:00
        var start = Local(2026, 3, 28, 22, 0);

        var until = Compute("MDJ106", start).Until;

        Assert.AreEqual(new DateTimeOffset(2026, 3, 29, 4, 0, 0, TimeSpan.FromHours(2)), until);
        Assert.AreEqual(TimeSpan.FromHours(5), until - start);
    }

    [TestMethod]
    public void UntilFourNextDay_AcrossAutumnDstChange()
    {
        // 25 October 2026: clocks go 03:00 -> 02:00
        var start = Local(2026, 10, 24, 22, 0);

        var until = Compute("MDJ106", start).Until;

        Assert.AreEqual(new DateTimeOffset(2026, 10, 25, 4, 0, 0, TimeSpan.FromHours(1)), until);
        Assert.AreEqual(TimeSpan.FromHours(7), until - start);
    }

    [TestMethod]
    public void FixedDuration_CountsElapsedTime_AcrossDst()
    {
        var start = Local(2026, 3, 28, 12, 0);

        var until = Compute("PHA24h", start).Until;

        Assert.AreEqual(TimeSpan.FromHours(24), until - start);
        Assert.AreEqual(new DateTimeOffset(2026, 3, 29, 13, 0, 0, TimeSpan.FromHours(2)), until);
    }

    [TestMethod]
    public void WorkingDayDuration_DuringDstGap()
    {
        // Sunday 01:30 CET, 70 minutes elapsed crosses the missing hour
        var start = new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.FromHours(1));

        var until = Compute("DPMO", start).Until;

        Assert.AreEqual(new DateTimeOffset(2026, 3, 29, 3, 40, 0, TimeSpan.FromHours(2)), until);
    }

    [TestMethod]
    public void DailyWindow_WithinWindow_ValidUntilEvening()
    {
        var start = Local(2026, 11, 21, 10, 0);

        var period = Compute("CHD", start);

        Assert.AreEqual(start, period.From);
        Assert.AreEqual(Local(2026, 11, 21, 23, 30), period.Until);
    }

    [TestMethod]
    public void DailyWindow_BeforeOpening_StartsAtOpening()
    {
        var period = Compute("CHD", Local(2026, 11, 21, 3, 0));

        Assert.AreEqual(Local(2026, 11, 21, 4, 30), period.From);
        Assert.AreEqual(Local(2026, 11, 21, 23, 30), period.Until);
    }

    [TestMethod]
    public void DailyWindow_AtBoundaries()
    {
        Assert.AreEqual(Local(2026, 11, 21, 4, 30), Compute("CHD", Local(2026, 11, 21, 4, 30)).From);

        var closing = Compute("CHD", Local(2026, 11, 21, 23, 30));
        Assert.AreEqual(TimeSpan.Zero, closing.Until - closing.From);
    }

    [TestMethod]
    public void DailyWindow_AfterClosing_HasNoValidity()
    {
        var period = Compute("CHD", Local(2026, 11, 21, 23, 45));

        Assert.IsTrue(period.Until <= period.From);
    }

    [TestMethod]
    [DataRow("PHA24h", true)]
    [DataRow("PHA72h", true)]
    [DataRow("PHA30min", false)]
    [DataRow("PHA90min", false)]
    [DataRow("BR99", false)]
    public void PragueLongTickets_RequireConfirmation(string ticketId, bool expected) =>
        Assert.AreEqual(expected, TicketValidityRules.Find(ticketId)!.RequiresConfirmation);

    private static ValidityPeriod Compute(string ticketId, DateTimeOffset start)
    {
        var rule = TicketValidityRules.Find(ticketId);
        Assert.IsNotNull(rule, ticketId);
        return rule.Compute(start, Prague);
    }

    internal static DateTimeOffset Local(int year, int month, int day, int hour, int minute)
    {
        DateTime local = new(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Prague.GetUtcOffset(local));
    }

    [GeneratedRegex(@"^(\d+) (min|h)$")]
    private static partial Regex TimeTextRegex();
}
