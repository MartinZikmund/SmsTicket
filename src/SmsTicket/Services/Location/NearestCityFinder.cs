using SmsTicket.Data.Models;

namespace SmsTicket.Services.Location;

public static class NearestCityFinder
{
    public const double DefaultRadiusKm = 30;

    // Distances closer than a metre count as equal, so float noise can't break ties
    private const double TieToleranceKm = 0.001;

    /// <summary>
    /// Returns the city with the closest centre within <paramref name="radiusKm"/>, or null.
    /// On a tie the city listed first wins.
    /// </summary>
    public static City? FindNearest(IEnumerable<City> cities, GeoCoordinate position, double radiusKm = DefaultRadiusKm)
    {
        City? nearest = null;
        var nearestDistance = double.MaxValue;

        foreach (var city in cities)
        {
            foreach (var center in city.Centers)
            {
                var distance = GeoDistance.Kilometers(position, center);
                if (distance <= radiusKm && distance < nearestDistance - TieToleranceKm)
                {
                    nearest = city;
                    nearestDistance = distance;
                }
            }
        }

        return nearest;
    }
}
