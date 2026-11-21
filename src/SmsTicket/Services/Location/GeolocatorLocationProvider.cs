using SmsTicket.Data.Models;
using Windows.Devices.Geolocation;

namespace SmsTicket.Services.Location;

public class GeolocatorLocationProvider : ILocationProvider
{
    // A city-level fix is enough, so a cached position is fine
    private static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PositionTimeout = TimeSpan.FromSeconds(10);

    // Uno implements Geolocator on Android, iOS and WebAssembly only; WinAppSDK has it natively
#if !HAS_UNO || __ANDROID__ || __IOS__ || __WASM__
    public bool IsSupported => true;
#else
    public bool IsSupported => false;
#endif

    public async Task<LocationResult> GetLocationAsync(bool allowPrompt, CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return LocationResult.Unsupported;
        }

        try
        {
            if (!await HasAccessAsync(allowPrompt, cancellationToken))
            {
                return LocationResult.Denied;
            }

            Geolocator geolocator = new() { DesiredAccuracy = PositionAccuracy.Default };
            var position = await geolocator.GetGeopositionAsync(MaximumAge, PositionTimeout).AsTask(cancellationToken);

            return position?.Coordinate?.Point?.Position is { } point
                ? LocationResult.Success(new GeoCoordinate(point.Latitude, point.Longitude))
                : LocationResult.Unavailable;
        }
        catch (NotImplementedException)
        {
            return LocationResult.Unsupported;
        }
        catch (UnauthorizedAccessException)
        {
            return LocationResult.Denied;
        }
    }

    private static async Task<bool> HasAccessAsync(bool allowPrompt, CancellationToken cancellationToken)
    {
#if __ANDROID__
        if (!Geolocator.IsLocationEnabled())
        {
            return false;
        }

        // Uno's Geolocator.RequestAccessAsync only asks for ACCESS_FINE_LOCATION, which Android 12+ ignores
        // when requested alone and which fails when the user picks "approximate". Coarse is all we need.
        return allowPrompt
            ? await Windows.Extensions.PermissionsHelper.TryGetCoarseLocationPermission(cancellationToken)
            : await Windows.Extensions.PermissionsHelper.CheckCoarseLocationPermission(cancellationToken);
#else
#if __IOS__
        if (!allowPrompt && new CoreLocation.CLLocationManager().AuthorizationStatus == CoreLocation.CLAuthorizationStatus.NotDetermined)
        {
            return false;
        }
#endif
        // Only prompts while the decision is undetermined, later calls just report the stored answer
        return await Geolocator.RequestAccessAsync().AsTask(cancellationToken) == GeolocationAccessStatus.Allowed;
#endif
    }
}
