namespace SmsTicket.Configuration;

public record AppConfig
{
    public string? Environment { get; init; }

    public string? AndroidAdUnitId { get; init; }

    public string? IosAdUnitId { get; init; }

    public string? AndroidTestDeviceId { get; init; }
}

