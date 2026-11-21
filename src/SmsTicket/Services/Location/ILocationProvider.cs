namespace SmsTicket.Services.Location;

public interface ILocationProvider
{
    bool IsSupported { get; }

    /// <param name="allowPrompt">When false, the user is never asked for permission.</param>
    Task<LocationResult> GetLocationAsync(bool allowPrompt, CancellationToken cancellationToken);
}
