namespace SmsTicket.Services.AppSettings;

// Values are stored by name and double as RadioButtons indexes - keep the order in sync with SettingsPage
public enum ThemePreference
{
    System,
    Light,
    Dark,
}

public enum LanguagePreference
{
    System,
    Czech,
    English,
}

public static class PreferenceExtensions
{
    public static ElementTheme ToElementTheme(this ThemePreference preference) => preference switch
    {
        ThemePreference.Light => ElementTheme.Light,
        ThemePreference.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    // An empty override means "follow the OS"
    public static string ToLanguageTag(this LanguagePreference preference) => preference switch
    {
        LanguagePreference.Czech => "cs",
        LanguagePreference.English => "en",
        _ => string.Empty,
    };
}
