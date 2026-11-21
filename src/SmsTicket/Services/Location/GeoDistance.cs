using SmsTicket.Data.Models;

namespace SmsTicket.Services.Location;

public static class GeoDistance
{
    // IUGG mean Earth radius
    public const double EarthRadiusKm = 6371.0088;

    /// <summary>Great-circle distance using the haversine formula.</summary>
    public static double Kilometers(GeoCoordinate from, GeoCoordinate to)
    {
        var lat1 = ToRadians(from.Latitude);
        var lat2 = ToRadians(to.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        // Clamp guards against rounding just above 1 for antipodal points
        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(Math.Min(1, h)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
