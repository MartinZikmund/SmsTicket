using System.Globalization;
using SmsTicket.Services.ActiveTickets;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class ActiveTicketFormatterTests
{
    private static readonly RemainingTimeFormats Formats = new("{0} min", "{0} h {1} min", "{0} d {1} h");

    [TestMethod]
    [DataRow(23 * 60, "23 min")]
    [DataRow(22 * 60 + 1, "23 min")] // rounds up so a valid ticket never shows less than it has
    [DataRow(30, "1 min")]
    [DataRow(60 * 60, "60 min")]
    [DataRow(61 * 60, "1 h 1 min")]
    [DataRow(23 * 3600 + 59 * 60, "23 h 59 min")]
    [DataRow(24 * 3600, "1 d 0 h")]
    [DataRow(25 * 3600 + 60, "1 d 1 h")]
    [DataRow(71 * 3600 + 30 * 60, "2 d 23 h")]
    public void Remaining(int seconds, string expected) =>
        Assert.AreEqual(expected, ActiveTicketFormatter.FormatRemaining(TimeSpan.FromSeconds(seconds), Formats, CultureInfo.InvariantCulture));

    [TestMethod]
    public void Until_SameDay_ShowsTimeOnly()
    {
        var culture = CultureInfo.GetCultureInfo("cs-CZ");
        var now = Local(2026, 11, 21, 14, 30);

        var text = ActiveTicketFormatter.FormatUntil(Local(2026, 11, 21, 14, 52), now, PragueTimeZone, culture);

        Assert.AreEqual("14:52", text);
    }

    [TestMethod]
    public void Until_OtherDay_IncludesDay()
    {
        var culture = CultureInfo.GetCultureInfo("cs-CZ");
        var now = Local(2026, 11, 21, 14, 30);

        var text = ActiveTicketFormatter.FormatUntil(Local(2026, 11, 22, 4, 0), now, PragueTimeZone, culture);

        Assert.AreEqual("ne 4:00", text);
    }

    private static TimeZoneInfo PragueTimeZone => SmsTicket.Data.PragueTime.Zone;

    [TestMethod]
    public void Range_SameDay_ShowsDateOnce() =>
        Assert.AreEqual(
            "11/21/2026 14:22–14:52",
            ActiveTicketFormatter.FormatRange(Local(2026, 11, 21, 14, 22), Local(2026, 11, 21, 14, 52), PragueTimeZone, CultureInfo.InvariantCulture));

    [TestMethod]
    public void Range_AcrossMidnight_ShowsBothDates() =>
        Assert.AreEqual(
            "11/21/2026 23:40 – 11/22/2026 01:10",
            ActiveTicketFormatter.FormatRange(Local(2026, 11, 21, 23, 40), Local(2026, 11, 22, 1, 10), PragueTimeZone, CultureInfo.InvariantCulture));
}
