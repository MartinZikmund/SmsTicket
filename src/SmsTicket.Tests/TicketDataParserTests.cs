using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public class TicketDataParserTests
{
    [TestMethod]
    public void Parse_ValidDocument_ResolvesBothLanguages()
    {
        var json = TicketDataJson.Build(cities: TicketDataJson.City(tickets: TicketDataJson.Ticket(
            description: """{ "cs": "Zlevněné jízdné", "en": "Reduced fare" }""",
            timeText: """{ "cs": "1 den", "en": "1 day" }""")));

        Assert.IsTrue(TicketDataParser.TryParse(json, out var document));

        var cs = TicketDataMapper.ToCities(document!, "cs").Single();
        var en = TicketDataMapper.ToCities(document!, "en").Single();

        Assert.AreEqual("Praha", cs.Name);
        Assert.AreEqual("PHA", cs.Abbreviation);
        Assert.AreEqual("Zlevněné jízdné", cs.TicketTypes[0].Description);
        Assert.AreEqual("1 den", cs.TicketTypes[0].TimeText);
        Assert.AreEqual("Prague", en.Name);
        Assert.AreEqual("PRG", en.Abbreviation);
        Assert.AreEqual("Reduced fare", en.TicketTypes[0].Description);
        Assert.AreEqual("1 day", en.TicketTypes[0].TimeText);
    }

    [TestMethod]
    public void Parse_MapsTicketFields()
    {
        Assert.IsTrue(TicketDataParser.TryParse(TicketDataJson.Build(), out var document));

        var ticket = TicketDataMapper.ToCities(document!, "cs").Single().TicketTypes.Single();

        Assert.AreEqual("T1", ticket.Id);
        Assert.AreEqual("42", ticket.Price);
        Assert.AreEqual("30 min", ticket.TimeText);
        Assert.AreEqual("90206", ticket.PhoneNumber);
        Assert.AreEqual("DPT42", ticket.SmsText);
        Assert.IsTrue(ticket.SmsPriceAdditional);
        Assert.IsFalse(ticket.HasDescription);
        Assert.AreEqual(new DateOnly(2026, 1, 1), document!.DataVersion);
    }

    [TestMethod]
    public void Parse_MissingTimeText_IsDuplicateTicket()
    {
        var json = TicketDataJson.Build(cities: TicketDataJson.City(tickets: TicketDataJson.Ticket(timeText: null)));

        Assert.IsTrue(TicketDataParser.TryParse(json, out var document));
        Assert.IsTrue(TicketDataMapper.ToCities(document!, "cs").Single().TicketTypes.Single().IsDuplicate);
    }

    [TestMethod]
    [DataRow("en-US", "Prague")]
    [DataRow("cs-CZ", "Praha")]
    [DataRow("de", "Prague")]
    public void Mapper_UnknownOrRegionalLanguage_FallsBack(string language, string expected)
    {
        Assert.IsTrue(TicketDataParser.TryParse(TicketDataJson.Build(), out var document));
        Assert.AreEqual(expected, TicketDataMapper.ToCities(document!, language).Single().Name);
    }

    [TestMethod]
    public void Mapper_SingleLanguageText_UsedForAnyLanguage()
    {
        var json = TicketDataJson.Build(cities: TicketDataJson.City(name: """{ "cs": "Kolín" }"""));

        Assert.IsTrue(TicketDataParser.TryParse(json, out var document));
        Assert.AreEqual("Kolín", TicketDataMapper.ToCities(document!, "en").Single().Name);
    }

    [TestMethod]
    public void Mapper_SortsCitiesByName()
    {
        var json = TicketDataJson.Build(cities: string.Join(",",
            TicketDataJson.City(id: "ZL", name: "\"Zlín\"", tickets: TicketDataJson.Ticket(id: "Z1"), defaultTicketId: "Z1"),
            TicketDataJson.City(id: "BR", name: "\"Brno\"", tickets: TicketDataJson.Ticket(id: "B1"), defaultTicketId: "B1")));

        Assert.IsTrue(TicketDataParser.TryParse(json, out var document));
        CollectionAssert.AreEqual(
            new[] { "BR", "ZL" },
            TicketDataMapper.ToCities(document!, "cs").Select(c => c.Id).ToArray());
    }

    [TestMethod]
    [DataRow("not json at all")]
    [DataRow("")]
    [DataRow("{ \"schemaVersion\": 1 }")]
    [DataRow("{ \"schemaVersion\": 1, \"dataVersion\": \"yesterday\", \"cities\": [] }")]
    public void Parse_MalformedJson_Fails(string json) =>
        Assert.IsFalse(TicketDataParser.TryParse(json, out _));

    [TestMethod]
    public void Parse_UnsupportedSchemaVersion_Fails() =>
        Assert.IsFalse(TicketDataParser.TryParse(TicketDataJson.Build(schemaVersion: 99), out _));

    [TestMethod]
    public void Parse_NoCities_Fails() =>
        Assert.IsFalse(TicketDataParser.TryParse(TicketDataJson.Build(cities: ""), out _));

    [TestMethod]
    public void Parse_MissingDefaultTicket_Fails() =>
        Assert.IsFalse(TicketDataParser.TryParse(
            TicketDataJson.Build(cities: TicketDataJson.City(defaultTicketId: "Nope")), out _));

    [TestMethod]
    public void Parse_DuplicateTicketIdsAcrossCities_Fails()
    {
        var json = TicketDataJson.Build(cities: string.Join(",",
            TicketDataJson.City(id: "A"),
            TicketDataJson.City(id: "B")));

        Assert.IsFalse(TicketDataParser.TryParse(json, out _));
    }

    [TestMethod]
    public void Parse_DuplicateCityIds_Fails()
    {
        var json = TicketDataJson.Build(cities: string.Join(",",
            TicketDataJson.City(id: "A", tickets: TicketDataJson.Ticket(id: "T1")),
            TicketDataJson.City(id: "A", tickets: TicketDataJson.Ticket(id: "T2"), defaultTicketId: "T2")));

        Assert.IsFalse(TicketDataParser.TryParse(json, out _));
    }

    [TestMethod]
    [DataRow("\"42\"")]
    [DataRow("42.5")]
    [DataRow("-1")]
    [DataRow("null")]
    public void Parse_NonNumericPrice_Fails(string price) =>
        Assert.IsFalse(TicketDataParser.TryParse(
            TicketDataJson.Build(cities: TicketDataJson.City(tickets: TicketDataJson.Ticket(price: price))), out _));

    [TestMethod]
    [DataRow("+420 90206")]
    [DataRow("90206a")]
    [DataRow("")]
    public void Parse_NonDigitPhoneNumber_Fails(string phone) =>
        Assert.IsFalse(TicketDataParser.TryParse(
            TicketDataJson.Build(cities: TicketDataJson.City(tickets: TicketDataJson.Ticket(phoneNumber: phone))), out _));

    [TestMethod]
    [DataRow("http://www.dpp.cz/")]
    [DataRow("javascript:alert(1)")]
    [DataRow("")]
    public void Parse_NonHttpsLink_Fails(string url) =>
        Assert.IsFalse(TicketDataParser.TryParse(
            TicketDataJson.Build(cities: TicketDataJson.City(operatorUrl: url)), out _));

    [TestMethod]
    public void Parse_EmptyLocalizedName_Fails() =>
        Assert.IsFalse(TicketDataParser.TryParse(
            TicketDataJson.Build(cities: TicketDataJson.City(name: """{ "cs": "" }""")), out _));
}
