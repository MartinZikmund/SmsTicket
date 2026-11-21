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

    public ThemePreference Theme
    {
        get => GetEnum(nameof(Theme), ThemePreference.System);
        set => _settings.SetSetting(nameof(Theme), value.ToString());
    }

    public LanguagePreference Language
    {
        get => GetEnum(nameof(Language), LanguagePreference.System);
        set => _settings.SetSetting(nameof(Language), value.ToString());
    }

    private TEnum GetEnum<TEnum>(string key, TEnum defaultValue) where TEnum : struct, Enum
    {
        var stored = _settings.GetSetting(key, () => defaultValue.ToString());

        // Reject numeric strings, which Enum.TryParse happily accepts
        return Enum.TryParse(stored, out TEnum value) && Enum.IsDefined(value) && !int.TryParse(stored, out _)
            ? value
            : defaultValue;
    }
}
