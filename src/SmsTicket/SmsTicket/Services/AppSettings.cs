using SmsTicket.Services.Settings;

namespace SmsTicket.Services.AppSettings;

public class AppSettings : IAppSettings
{
    private readonly ISettingsService _settings;

    public AppSettings(ISettingsService settings)
    {
        _settings = settings;
    }

    public bool FirstTimeUse
    {
        get => _settings.GetSetting(nameof(FirstTimeUse), () => true);
        set => _settings.SetSetting(nameof(FirstTimeUse), value);
    }
}
