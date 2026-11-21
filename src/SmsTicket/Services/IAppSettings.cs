namespace SmsTicket.Services.AppSettings;

public interface IAppSettings
{
    bool FirstTimeUse { get; set; }

    bool AutoSelectNearestCity { get; set; }

    bool HasUsedNearestCity { get; set; }
}
