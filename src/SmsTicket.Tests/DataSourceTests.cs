using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public class DataSourceTests
{
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
}
