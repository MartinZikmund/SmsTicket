using SmsTicket.Data.Models;
using SmsTicket.Services.Location;

namespace SmsTicket.Tests;

[TestClass]
public class GeoDistanceTests
{
    private static readonly GeoCoordinate Prague = new(50.0875, 14.4213);
    private static readonly GeoCoordinate Brno = new(49.1951, 16.6083);

    [TestMethod]
    public void SamePoint_IsZero() => Assert.AreEqual(0, GeoDistance.Kilometers(Prague, Prague), 1e-9);

    [TestMethod]
    public void OneDegreeOfLatitude_IsAbout111Km() =>
        Assert.AreEqual(111.19, GeoDistance.Kilometers(new(49, 15), new(50, 15)), 0.05);

    [TestMethod]
    public void PragueToBrno_IsAbout185Km() =>
        Assert.AreEqual(185, GeoDistance.Kilometers(Prague, Brno), 2);

    [TestMethod]
    public void IsSymmetric() =>
        Assert.AreEqual(GeoDistance.Kilometers(Prague, Brno), GeoDistance.Kilometers(Brno, Prague), 1e-9);

    [TestMethod]
    public void Antipodes_IsHalfTheEarthCircumference() =>
        Assert.AreEqual(Math.PI * GeoDistance.EarthRadiusKm, GeoDistance.Kilometers(new(0, 0), new(0, 180)), 1e-6);
}
