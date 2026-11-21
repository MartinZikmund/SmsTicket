using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Location;

namespace SmsTicket.Tests;

[TestClass]
public class NearestCityFinderTests
{
    private static City CreateCity(string id, params GeoCoordinate[] centers) => new()
    {
        Id = id,
        Name = id,
        Abbreviation = id,
        DefaultTicketId = "",
        TicketTypes = new(),
        OperatorName = "",
        OperatorUrl = "",
        SmsInfoUrl = "",
        JourneyPlannerUrl = "",
        Centers = centers,
    };

    private static City CityById(string id) => DataSource.Cities.Single(c => c.Id == id);

    [TestMethod]
    public void PicksNearestCity()
    {
        var a = CreateCity("A", new GeoCoordinate(50.0, 15.0));
        var b = CreateCity("B", new GeoCoordinate(50.1, 15.0));

        var result = NearestCityFinder.FindNearest([a, b], new(50.08, 15.0));

        Assert.AreSame(b, result);
    }

    [TestMethod]
    public void ReturnsNull_WhenNoCityWithinRadius()
    {
        var a = CreateCity("A", new GeoCoordinate(50.0, 15.0));

        // ~33 km north
        Assert.IsNull(NearestCityFinder.FindNearest([a], new(50.3, 15.0)));
    }

    [TestMethod]
    public void IncludesCity_JustInsideRadius()
    {
        var a = CreateCity("A", new GeoCoordinate(50.0, 15.0));

        // ~27.8 km north
        Assert.AreSame(a, NearestCityFinder.FindNearest([a], new(50.25, 15.0)));
    }

    [TestMethod]
    public void RespectsCustomRadius()
    {
        var a = CreateCity("A", new GeoCoordinate(50.0, 15.0));

        Assert.IsNull(NearestCityFinder.FindNearest([a], new(50.1, 15.0), radiusKm: 5));
        Assert.AreSame(a, NearestCityFinder.FindNearest([a], new(50.1, 15.0), radiusKm: 15));
    }

    [TestMethod]
    public void Tie_PrefersFirstCityInList()
    {
        var north = CreateCity("N", new GeoCoordinate(50.1, 15.0));
        var south = CreateCity("S", new GeoCoordinate(49.9, 15.0));

        Assert.AreSame(north, NearestCityFinder.FindNearest([north, south], new(50.0, 15.0)));
        Assert.AreSame(south, NearestCityFinder.FindNearest([south, north], new(50.0, 15.0)));
    }

    [TestMethod]
    public void CityWithoutCenters_IsIgnored()
    {
        var empty = CreateCity("E");

        Assert.IsNull(NearestCityFinder.FindNearest([empty], new(50.0, 15.0)));
    }

    [TestMethod]
    public void MultiCenterCity_UsesClosestCenter()
    {
        var twin = CreateCity("T", new GeoCoordinate(50.0, 15.0), new GeoCoordinate(50.0, 16.0));
        var other = CreateCity("O", new GeoCoordinate(50.0, 15.6));

        // 0.1° from the second centre, 0.5° from the other city
        Assert.AreSame(twin, NearestCityFinder.FindNearest([twin, other], new(50.0, 15.9)));
    }

    [TestMethod]
    public void Otrokovice_SelectsZlin() =>
        Assert.AreSame(CityById("ZL"), NearestCityFinder.FindNearest(DataSource.Cities, new(49.2093, 17.5308)));

    [TestMethod]
    public void WestOfOtrokovice_OutsideZlinRadius_StillSelectsZlin()
    {
        GeoCoordinate position = new(49.2093, 17.19);
        var zlin = CityById("ZL");

        Assert.IsTrue(GeoDistance.Kilometers(position, zlin.Centers[0]) > NearestCityFinder.DefaultRadiusKm);
        Assert.AreSame(zlin, NearestCityFinder.FindNearest(DataSource.Cities, position));
    }

    [TestMethod]
    public void PragueOldTownSquare_SelectsPrague() =>
        Assert.AreSame(CityById("PHA"), NearestCityFinder.FindNearest(DataSource.Cities, new(50.0870, 14.4208)));

    [TestMethod]
    public void BetweenHradecAndPardubice_SelectsTheCloserOne()
    {
        // Opatovice nad Labem, closer to Hradec Králové
        Assert.AreSame(CityById("HK"), NearestCityFinder.FindNearest(DataSource.Cities, new(50.146, 15.79)));
    }

    [TestMethod]
    public void Jihlava_HasNoSupportedCityNearby() =>
        Assert.IsNull(NearestCityFinder.FindNearest(DataSource.Cities, new(49.3961, 15.5912)));

    [TestMethod]
    public void Vienna_HasNoSupportedCityNearby() =>
        Assert.IsNull(NearestCityFinder.FindNearest(DataSource.Cities, new(48.2082, 16.3738)));
}
