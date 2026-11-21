namespace SmsTicket.Views;

/// <summary>
/// Top-level destinations shared by the wide navigation pane and the bottom tab bar, keyed by item Tag.
/// </summary>
public static class ShellDestinations
{
    public const string Tickets = nameof(Tickets);
    public const string History = nameof(History);
    public const string Settings = nameof(Settings);
    public const string About = nameof(About);

    private static readonly Dictionary<string, Type> _pages = new()
    {
        [Tickets] = typeof(MainPage),
        [History] = typeof(HistoryPage),
        [Settings] = typeof(SettingsPage),
        [About] = typeof(AboutPage),
    };

    public static Type? PageFor(string? tag) => tag is not null && _pages.TryGetValue(tag, out var page) ? page : null;

    public static string? TagFor(Type? page) => _pages.FirstOrDefault(p => p.Value == page).Key;

    /// <summary>
    /// The bottom tab that owns a destination. About is reached from Settings, so it shares its tab.
    /// </summary>
    public static string? TabFor(string? tag) => tag == About ? Settings : tag;
}
