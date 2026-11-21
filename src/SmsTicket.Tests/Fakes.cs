using SmsTicket.Data.Models;
using SmsTicket.Services.Location;

namespace SmsTicket.Tests;

internal sealed class FakeLocationProvider : ILocationProvider
{
    private readonly Func<bool, Task<LocationResult>> _getLocation;

    public FakeLocationProvider(Func<bool, Task<LocationResult>> getLocation, bool isSupported = true)
    {
        _getLocation = getLocation;
        IsSupported = isSupported;
    }

    public static FakeLocationProvider Returning(LocationResult result, bool isSupported = true) =>
        new(_ => Task.FromResult(result), isSupported);

    public static FakeLocationProvider At(double latitude, double longitude) =>
        Returning(LocationResult.Success(new GeoCoordinate(latitude, longitude)));

    public bool IsSupported { get; }

    public List<bool> Calls { get; } = new();

    public Task<LocationResult> GetLocationAsync(bool allowPrompt, CancellationToken cancellationToken)
    {
        Calls.Add(allowPrompt);
        return _getLocation(allowPrompt);
    }
}
