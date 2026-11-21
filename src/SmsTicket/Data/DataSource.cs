using System.Globalization;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;

namespace SmsTicket.Data;

/// <summary>
/// Ticket data from data/tickets.json: the copy bundled with the app, or a newer one downloaded from GitHub.
/// </summary>
public static class DataSource
{
    public const string RemoteUrl = "https://raw.githubusercontent.com/MartinZikmund/SmsTicket/main/data/tickets.json";

    private const string BundledResourceName = "SmsTicket.Data.tickets.json";
    private const string CacheFileName = "tickets.json";

    private static readonly Lazy<TicketDataRepository> _repository = new(CreateRepository);

    // Loaded once per session - a download made meanwhile applies on the next launch,
    // so the city and ticket lists never change while the user is picking from them
    private static readonly Lazy<IReadOnlyList<City>> _cities = new(
        () => TicketDataMapper.ToCities(_repository.Value.LoadCurrent(), GetLanguage()));

    public static IReadOnlyList<City> Cities => _cities.Value;

    public static Task RefreshAsync() => _repository.Value.RefreshAsync();

    public static Stream OpenBundled() =>
        typeof(DataSource).Assembly.GetManifestResourceStream(BundledResourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource {BundledResourceName}");

    private static TicketDataRepository CreateRepository()
    {
        HttpClient httpClient = new() { Timeout = TicketDataRepository.DefaultTimeout };
        return new TicketDataRepository(OpenBundled, GetCachePath(), httpClient, new Uri(RemoteUrl));
    }

    private static string? GetCachePath()
    {
        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, CacheFileName);
        }
        catch (Exception)
        {
            // No app data folder (e.g. unit tests) - run on bundled data only
            return null;
        }
    }

    // The resw files say which language the UI resolved to, so data texts always match it
    private static string GetLanguage()
    {
        const string key = "DataLanguage";
        var language = new LocalizationService()[key];
        return language == key ? CultureInfo.CurrentUICulture.Name : language;
    }

    public static CityTicket? FindTicket(string? ticketId)
    {
        foreach (var city in Cities)
        {
            if (city.TicketTypes.FirstOrDefault(t => t.Id == ticketId) is { } ticket)
            {
                return new CityTicket(city, ticket);
            }
        }

        return null;
    }

    public static City? FindCityForTicket(string ticketId) =>
        Cities.FirstOrDefault(c => c.TicketTypes.Any(t => t.Id == ticketId));
}
