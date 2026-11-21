using SmsTicket.Data;
using SmsTicket.Services.Location;

namespace SmsTicket.Tests;

[TestClass]
public class CityLocationTests
{
    // Czech Republic bounding box
    private const double MinLatitude = 48.55;
    private const double MaxLatitude = 51.06;
    private const double MinLongitude = 12.09;
    private const double MaxLongitude = 18.86;

    [TestMethod]
    public void EveryCity_HasCenterInsideCzechRepublic()
    {
        foreach (var city in DataSource.Cities)
        {
            Assert.IsNotEmpty(city.Centers, $"{city.Id}: no centre coordinates");
            foreach (var center in city.Centers)
            {
                Assert.IsTrue(
                    center.Latitude is >= MinLatitude and <= MaxLatitude &&
                    center.Longitude is >= MinLongitude and <= MaxLongitude,
                    $"{city.Id}: {center} is outside the Czech Republic");
            }
        }
    }

    [TestMethod]
    public void Zlin_HasZlinAndOtrokoviceCenters() =>
        Assert.HasCount(2, DataSource.Cities.Single(c => c.Id == "ZL").Centers);

    [TestMethod]
    public void EveryCity_SelectsItselfFromItsOwnCenter()
    {
        foreach (var city in DataSource.Cities)
        {
            foreach (var center in city.Centers)
            {
                Assert.AreSame(city, NearestCityFinder.FindNearest(DataSource.Cities, center), city.Id);
            }
        }
    }

    [TestMethod]
    [DataRow("PHA", 50.0875, 14.4213)]
    [DataRow("BR", 49.1951, 16.6083)]
    [DataRow("OST", 49.8356, 18.2925)]
    [DataRow("PIL", 49.7477, 13.3776)]
    [DataRow("CHEB", 50.0796, 12.3739)]
    public void KnownCityCenters_AreWithinOneKilometer(string cityId, double latitude, double longitude)
    {
        var city = DataSource.Cities.Single(c => c.Id == cityId);

        Assert.IsTrue(GeoDistance.Kilometers(city.Centers[0], new(latitude, longitude)) < 1, cityId);
    }
}
