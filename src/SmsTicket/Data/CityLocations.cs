using SmsTicket.Data.Models;

namespace SmsTicket.Data;

// Main square of each city, WGS84
public static class CityLocations
{
    private static readonly Dictionary<string, GeoCoordinate[]> _centers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PHA"] = [new(50.0875, 14.4213)], // Staroměstské náměstí
        ["BR"] = [new(49.1951, 16.6083)], // náměstí Svobody
        ["LBC"] = [new(50.7697, 15.0585)], // náměstí Dr. E. Beneše
        ["CB"] = [new(48.9745, 14.4743)], // náměstí Přemysla Otakara II.
        ["HK"] = [new(50.2092, 15.8326)], // Velké náměstí
        ["OL"] = [new(49.5938, 17.2509)], // Horní náměstí
        ["OST"] = [new(49.8356, 18.2925)], // Masarykovo náměstí
        ["PAR"] = [new(50.0381, 15.7787)], // Pernštýnské náměstí
        ["PIL"] = [new(49.7477, 13.3776)], // náměstí Republiky
        ["UL"] = [new(50.6602, 14.0404)], // Mírové náměstí
        ["ZL"] = [new(49.2265, 17.6670), new(49.2093, 17.5308)], // náměstí Míru, Zlín; náměstí 3. května, Otrokovice
        ["TB"] = [new(49.4144, 14.6578)], // Žižkovo náměstí
        ["CHEB"] = [new(50.0796, 12.3739)], // náměstí Krále Jiřího z Poděbrad
    };

    public static IReadOnlyList<GeoCoordinate> ForCity(string cityId) => _centers.GetValueOrDefault(cityId) ?? [];
}
