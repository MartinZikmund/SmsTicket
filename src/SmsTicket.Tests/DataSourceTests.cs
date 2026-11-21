using SmsTicket.Data;

namespace SmsTicket.Tests;

// Validates the shipped data/tickets.json, which DataSource loads from its embedded copy
[TestClass]
public class DataSourceTests
{
    [TestMethod]
    public void ShippedJson_IsValid()
    {
        var json = File.ReadAllText(FindShippedJson());

        Assert.IsTrue(TicketDataParser.TryParse(json, out var document, out var error), error);
        Assert.AreEqual(TicketDataParser.SupportedSchemaVersion, document!.SchemaVersion);
    }

    [TestMethod]
    public void EmbeddedJson_MatchesShippedFile()
    {
        using var stream = DataSource.OpenBundled();
        using StreamReader reader = new(stream);

        Assert.AreEqual(File.ReadAllText(FindShippedJson()), reader.ReadToEnd());
    }

    [TestMethod]
    public void ShippedJson_LocalizedTexts_HaveCzechAndEnglish()
    {
        Assert.IsTrue(TicketDataParser.TryParse(File.ReadAllText(FindShippedJson()), out var document));

        foreach (var city in document!.Cities)
        {
            var texts = new[] { city.Name, city.Abbreviation }
                .Concat(city.Tickets.SelectMany(t => new[] { t.Description, t.TimeText }))
                .OfType<LocalizedText>()
                .Where(t => !t.IsInvariant);

            foreach (var text in texts)
            {
                Assert.IsTrue(
                    text.Values.ContainsKey("cs") && text.Values.ContainsKey("en"),
                    $"{city.Id}: '{text}' is missing a Czech or English translation");
            }
        }
    }

    [TestMethod]
    public void Cities_AreLoaded() => Assert.AreEqual(13, DataSource.Cities.Count);
    [TestMethod]
    public void EveryCity_DefaultTicketExists()
    {
        foreach (var city in DataSource.Cities)
        {
            Assert.IsTrue(
                city.TicketTypes.Any(t => t.Id == city.DefaultTicketId),
                $"{city.Id}: default ticket '{city.DefaultTicketId}' does not exist");
        }
    }

    [TestMethod]
    public void TicketIds_AreUnique()
    {
        var duplicates = DataSource.Cities
            .SelectMany(c => c.TicketTypes)
            .GroupBy(t => t.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.AreEqual(0, duplicates.Count, $"Duplicate ticket ids: {string.Join(", ", duplicates)}");
    }

    [TestMethod]
    public void CityIds_AreUnique() =>
        Assert.AreEqual(DataSource.Cities.Count, DataSource.Cities.Select(c => c.Id).Distinct().Count());

    [TestMethod]
    public void EveryTicket_HasNumericPriceAndPhoneNumber()
    {
        foreach (var ticket in DataSource.Cities.SelectMany(c => c.TicketTypes))
        {
            Assert.IsTrue(int.TryParse(ticket.Price, out _), $"{ticket.Id}: price '{ticket.Price}'");
            Assert.IsTrue(ticket.PhoneNumber.All(char.IsDigit), $"{ticket.Id}: phone '{ticket.PhoneNumber}'");
            Assert.IsFalse(string.IsNullOrWhiteSpace(ticket.SmsText), $"{ticket.Id}: empty SMS text");
        }
    }

    [TestMethod]
    public void EveryCity_HasPhotoCredit()
    {
        foreach (var city in DataSource.Cities)
        {
            Assert.IsNotNull(city.Photo, $"{city.Id}: missing photo credit");
        }
    }

    [TestMethod]
    public void EveryCity_HasOperatorName()
    {
        foreach (var city in DataSource.Cities)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(city.OperatorName), $"{city.Id}: missing operator name");
        }
    }

    [TestMethod]
    public void EveryCity_HasAbsoluteHttpsLinks()
    {
        foreach (var city in DataSource.Cities)
        {
            AssertHttps(city.Id, nameof(city.OperatorUrl), city.OperatorUrl);
            AssertHttps(city.Id, nameof(city.SmsInfoUrl), city.SmsInfoUrl);
            AssertHttps(city.Id, nameof(city.JourneyPlannerUrl), city.JourneyPlannerUrl);
        }
    }

    private static string FindShippedJson()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "data", "tickets.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("data/tickets.json not found above " + AppContext.BaseDirectory);
    }

    private static void AssertHttps(string cityId, string name, string? url) =>
        Assert.IsTrue(
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
            $"{cityId}: {name} '{url}' is not an absolute https URL");
}
