using SmsTicket.Data;
using SmsTicket.Data.Models;

namespace SmsTicket.Tests;

[TestClass]
public class CityTests
{
    [TestMethod]
    public void CityWithBundledPhoto_HasHeroImageAndCredit()
    {
        var city = MapSingleCity("PHA");

        Assert.AreEqual("ms-appx:///Assets/Cities/pha.jpg", city.HeroImage);
        Assert.IsNotNull(city.Photo);
    }

    [TestMethod]
    public void CityWithoutBundledPhoto_HasNoHeroImageOrCredit()
    {
        // A city added remotely after the app shipped
        var city = MapSingleCity("KOL");

        Assert.IsNull(city.HeroImage);
        Assert.IsNull(city.Photo);
    }

    private static City MapSingleCity(string id)
    {
        Assert.IsTrue(TicketDataParser.TryParse(TicketDataJson.Build(cities: TicketDataJson.City(id: id)), out var document));
        return TicketDataMapper.ToCities(document!, "cs").Single();
    }
}
