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

    public bool AutoSelectNearestCity
    {
        get => _settings.GetSetting(nameof(AutoSelectNearestCity), () => false);
        set => _settings.SetSetting(nameof(AutoSelectNearestCity), value);
    }

    public bool HasUsedNearestCity
    {
        get => _settings.GetSetting(nameof(HasUsedNearestCity), () => false);
        set => _settings.SetSetting(nameof(HasUsedNearestCity), value);
    }
}
