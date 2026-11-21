namespace SmsTicket.Services.AppSettings;

public interface IAppSettings
{
    bool FirstTimeUse { get; set; }

    bool ReminderPermissionAsked { get; set; }

    bool AutoSelectNearestCity { get; set; }

    bool HasUsedNearestCity { get; set; }

    ThemePreference Theme { get; set; }

    LanguagePreference Language { get; set; }
}
