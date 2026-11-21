using SmsTicket.Data.Models;

namespace SmsTicket.Services.Location;

public enum LocationStatus
{
    Success,
    Denied,
    Unavailable,
    Unsupported,
}

public readonly record struct LocationResult(LocationStatus Status, GeoCoordinate? Position = null)
{
    public static LocationResult Denied => new(LocationStatus.Denied);

    public static LocationResult Unavailable => new(LocationStatus.Unavailable);

    public static LocationResult Unsupported => new(LocationStatus.Unsupported);

    public static LocationResult Success(GeoCoordinate position) => new(LocationStatus.Success, position);
}
