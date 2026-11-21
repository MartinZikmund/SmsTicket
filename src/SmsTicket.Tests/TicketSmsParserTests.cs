using SmsTicket.Data;
using SmsTicket.Services.Confirmation;
using static SmsTicket.Tests.TicketValidityRulesTests;

namespace SmsTicket.Tests;

[TestClass]
public class TicketSmsParserTests
{
    // Tarif PID 2026-01-01, art. XIX items 8-9 ("Vzor jízdního dokladu za 55 Kč"), PDF page 75:
    // https://pid.cz/wp-content/uploads/system/tarif_spp_historie/Tarif_PID_2026-01-01_komplet_d1_d2_d3_d4_d5_d6_d7.pdf
    // The Tarif only shows the label "kód pro kontrolu platnosti"; the value line below it is an assumed placeholder.
    internal const string PragueSample = """
        DP hl.m.Prahy, a.s.
        Jízdenka přestupní 55,- Kč
        Platnost:
        od 2.5.26. 13:20
        do 2.5.26. 14:50
        pásma P,0,B; mimo vlaky
        kód pro kontrolu platnosti
        """;

    // https://www.dpmb.cz/sms-jizdenky - verbatim, including the missing space in "25.11.202216:15"
    private const string BrnoSample = """
        DPMB, a.s., jízdenka přestupní 29 Kč, plati v zónách 100 + 101, mimo vlak.
        Platnost: od  25.11.2022 15:00 do: 25.11.202216:15, JZeCojhb/951970
        """;

    // https://www.dpmb.cz/en/mobile-phone-tickets - the English page's translation of the same ticket
    private const string BrnoEnglishSample = """
        DPMB, a.s., transfer ticket 29 CZK, valid in zones 100 + 101, excluding the train.
        Validity: from 25.11.2022 15:00 to: 25.11.2022 16:15, JZeCojhb/951970.
        """;

    // https://www.dpmo.cz/informace-pro-cestujici/sms-jizdenka/ ("Vzor SMS jízdenky")
    private const string OlomoucSample = """
        DP m. Olomouce, a.s.
        SMS jizdenka prestupni 27 Kc
        Platnost:
        Od: 3.9.23 15:03
        Do: 3.9.23 15:53
        Pouze linky DPMO
        mXpay6d5E/207885
        """;

    // https://www.dszo.cz/sms-jizdenka/ - verbatim, including "do." instead of "do:"
    private const string ZlinSample = """
        a3D / 123456
        DSZO, s.r.o.
        Jizdenka prestupna 30 Kc
        Platnost:
        od: 1.7.2014 12:00
        do. 1.7.2014 13:10
        YrQPtMks7
        """;

    private static readonly TimeZoneInfo Zone = PragueTime.Zone;

    [TestMethod]
    public void Prague_TarifSample()
    {
        var parsed = TicketSmsParser.Parse(PragueSample + "\nAB12CD34", Local(2026, 5, 2, 13, 19), Zone);

        Assert.AreEqual(Local(2026, 5, 2, 13, 20), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 5, 2, 14, 50), parsed.ValidUntil);
        Assert.AreEqual(55, parsed.Price);
        Assert.AreEqual("PHA", parsed.CityId);
        Assert.AreEqual("AB12CD34", parsed.Code);
    }

    [TestMethod]
    public void Prague_TarifSample_WithoutCodeValue()
    {
        var parsed = TicketSmsParser.Parse(PragueSample, Local(2026, 5, 2, 13, 19), Zone);

        Assert.IsTrue(parsed.HasValidity);
        Assert.IsNull(parsed.Code);
    }

    [TestMethod]
    public void Prague_Duplicate()
    {
        // Tarif PID: the duplicate differs only in the price line
        var duplicate = PragueSample.Replace("55,- Kč", "55,- Kč duplikát");

        var parsed = TicketSmsParser.Parse(duplicate, Local(2026, 5, 2, 14, 0), Zone);

        Assert.AreEqual(Local(2026, 5, 2, 14, 50), parsed.ValidUntil);
        Assert.AreEqual(55, parsed.Price);
    }

    [TestMethod]
    public void Brno_GluedYearAndHour()
    {
        var parsed = TicketSmsParser.Parse(BrnoSample, Local(2022, 11, 25, 14, 59), Zone);

        Assert.AreEqual(Local(2022, 11, 25, 15, 0), parsed.ValidFrom);
        Assert.AreEqual(Local(2022, 11, 25, 16, 15), parsed.ValidUntil);
        Assert.AreEqual(29, parsed.Price);
        Assert.AreEqual("BR", parsed.CityId);
        Assert.AreEqual("JZeCojhb/951970", parsed.Code);
    }

    [TestMethod]
    public void Brno_English()
    {
        var parsed = TicketSmsParser.Parse(BrnoEnglishSample, Local(2022, 11, 25, 14, 59), Zone);

        Assert.AreEqual(Local(2022, 11, 25, 15, 0), parsed.ValidFrom);
        Assert.AreEqual(Local(2022, 11, 25, 16, 15), parsed.ValidUntil);
        Assert.AreEqual(29, parsed.Price);
        Assert.AreEqual("JZeCojhb/951970", parsed.Code);
    }

    [TestMethod]
    public void Olomouc_Sample()
    {
        var parsed = TicketSmsParser.Parse(OlomoucSample, Local(2023, 9, 3, 15, 2), Zone);

        Assert.AreEqual(Local(2023, 9, 3, 15, 3), parsed.ValidFrom);
        Assert.AreEqual(Local(2023, 9, 3, 15, 53), parsed.ValidUntil);
        Assert.AreEqual(27, parsed.Price);
        Assert.AreEqual("OL", parsed.CityId);
        Assert.AreEqual("mXpay6d5E/207885", parsed.Code);
    }

    [TestMethod]
    public void Zlin_Sample()
    {
        var parsed = TicketSmsParser.Parse(ZlinSample, Local(2014, 7, 1, 11, 59), Zone);

        Assert.AreEqual(Local(2014, 7, 1, 12, 0), parsed.ValidFrom);
        Assert.AreEqual(Local(2014, 7, 1, 13, 10), parsed.ValidUntil);
        Assert.AreEqual(30, parsed.Price);
        Assert.AreEqual("ZL", parsed.CityId);
        Assert.AreEqual("a3D/123456", parsed.Code);
    }

    [TestMethod]
    public void DiacriticsStripped_SameResult()
    {
        const string stripped = """
            DP hl.m.Prahy, a.s.
            Jizdenka prestupni 55,- Kc
            Platnost:
            od 2.5.26. 13:20
            do 2.5.26. 14:50
            pasma P,0,B; mimo vlaky
            """;

        var parsed = TicketSmsParser.Parse(stripped, Local(2026, 5, 2, 13, 19), Zone);

        Assert.AreEqual(Local(2026, 5, 2, 13, 20), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 5, 2, 14, 50), parsed.ValidUntil);
        Assert.AreEqual(55, parsed.Price);
        Assert.AreEqual("PHA", parsed.CityId);
    }

    [TestMethod]
    public void SingleLine_FourDigitYear()
    {
        // Edge case, not a published format: all on one line
        var parsed = TicketSmsParser.Parse("Jizdenka 30 Kc platnost od 21.11.2026 14:22 do 21.11.2026 14:52", Local(2026, 11, 21, 14, 21), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 14, 22), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 21, 14, 52), parsed.ValidUntil);
    }

    [TestMethod]
    public void MidnightCrossing_WithDates()
    {
        const string sms = "DP hl.m.Prahy, a.s.\nJizdenka prestupni 55,- Kc\nPlatnost:\nod 21.11.26. 23:40\ndo 22.11.26. 1:10";

        var parsed = TicketSmsParser.Parse(sms, Local(2026, 11, 21, 23, 39), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 23, 40), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 22, 1, 10), parsed.ValidUntil);
    }

    [TestMethod]
    public void MidnightCrossing_TimesOnly()
    {
        // Edge case, not a published format: the dates are left out
        var parsed = TicketSmsParser.Parse("Jizdenka platnost od 23:40 do 0:10", Local(2026, 11, 21, 23, 39), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 23, 40), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 22, 0, 10), parsed.ValidUntil);
    }

    [TestMethod]
    public void TimesOnly_ReferenceJustAfterMidnight_StartIsPreviousDay()
    {
        var parsed = TicketSmsParser.Parse("Jizdenka platnost od 23:58 do 0:28", Local(2026, 11, 22, 0, 1), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 23, 58), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 22, 0, 28), parsed.ValidUntil);
    }

    [TestMethod]
    public void UntilOnly_TimeWithoutDate_IsNextOccurrenceAfterReference()
    {
        var parsed = TicketSmsParser.Parse("SMS jizdenka plati do 0:15", Local(2026, 11, 21, 23, 50), Zone);

        Assert.IsNull(parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 22, 0, 15), parsed.ValidUntil);
    }

    [TestMethod]
    public void TwoDigitYear_IsThisCentury()
    {
        var parsed = TicketSmsParser.Parse("Jizdenka\nOd: 31.12.99 23:00\nDo: 1.1.00 0:30", Local(2099, 12, 31, 22, 59), Zone);

        Assert.AreEqual(Local(2099, 12, 31, 23, 0), parsed.ValidFrom);
        Assert.AreEqual(Local(2100, 1, 1, 0, 30), parsed.ValidUntil);
    }

    [TestMethod]
    public void Seconds_AreAccepted()
    {
        var parsed = TicketSmsParser.Parse("Jizdenka platnost od 21.11.2026 14:22:31 do 21.11.2026 14:52:31", Local(2026, 11, 21, 14, 21), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 14, 22).AddSeconds(31), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 21, 14, 52).AddSeconds(31), parsed.ValidUntil);
    }

    [TestMethod]
    public void UnlabeledPair_IsFromAndUntil()
    {
        // Edge case, not a published format
        var parsed = TicketSmsParser.Parse("Jizdenka 30 Kc, platnost 21.11.26 14:22 - 21.11.26 14:52", Local(2026, 11, 21, 14, 21), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 14, 22), parsed.ValidFrom);
        Assert.AreEqual(Local(2026, 11, 21, 14, 52), parsed.ValidUntil);
    }

    [TestMethod]
    public void DaylightSavingChange_UsesPragueOffset()
    {
        // 25.10.2026 at 3:00 CEST the clocks go back to 2:00 CET
        var parsed = TicketSmsParser.Parse("Jizdenka\nOd: 25.10.26 1:30\nDo: 25.10.26 4:30", Local(2026, 10, 25, 1, 29), Zone);

        Assert.AreEqual(TimeSpan.FromHours(2), parsed.ValidFrom!.Value.Offset);
        Assert.AreEqual(TimeSpan.FromHours(1), parsed.ValidUntil!.Value.Offset);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("Ahoj, dorazim v 15:00, kup prosim rohliky")]
    [DataRow("Budu doma od 14:00 do 16:00")]
    [DataRow("Vas overovaci kod je 482913")]
    [DataRow("Platnost: od 99.99.26 25:61 do 32.13.26 24:00")]
    public void Garbage_HasNoValidity(string text)
    {
        var parsed = TicketSmsParser.Parse(text, Local(2026, 11, 21, 14, 0), Zone);

        Assert.IsFalse(parsed.HasValidity);
    }

    [TestMethod]
    public void UntilBeforeFrom_UntilDropped()
    {
        var parsed = TicketSmsParser.Parse("Jizdenka\nOd: 21.11.26 15:00\nDo: 21.11.26 14:00", Local(2026, 11, 21, 14, 59), Zone);

        Assert.AreEqual(Local(2026, 11, 21, 15, 0), parsed.ValidFrom);
        Assert.IsNull(parsed.ValidUntil);
    }
}
