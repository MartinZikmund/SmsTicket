using System.Globalization;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.PurchaseHistory;

namespace SmsTicket.ViewModels;

public class HistoryViewModel : ObservableObject
{
    private readonly IPurchaseHistoryService _history;
    private readonly LocalizationService _localizer;

    private IReadOnlyList<PurchaseHistoryMonth> _months = [];

    public HistoryViewModel(IPurchaseHistoryService history, LocalizationService localizer)
    {
        _history = history;
        _localizer = localizer;
    }

    public IReadOnlyList<PurchaseHistoryMonth> Months
    {
        get => _months;
        private set
        {
            if (SetProperty(ref _months, value))
            {
                OnPropertyChanged(nameof(HasItems));
            }
        }
    }

    public bool HasItems => Months.Count > 0;

    public async Task LoadAsync()
    {
        var records = await _history.GetAllAsync();
        var culture = CultureInfo.CurrentCulture;
        Months = PurchaseHistoryGrouping.GroupByMonth(records, TimeZoneInfo.Local)
            .Select(g => new PurchaseHistoryMonth(
                MonthHeader(g.Year, g.Month, culture),
                $"{_localizer.MonthTotal} {FormatPrice(g.TotalPrice)}",
                g.Items.Select(r => CreateItem(r, culture)).ToList()))
            .ToList();
    }

    public Task DeleteAsync(PurchaseHistoryItem item) => _history.DeleteAsync(item.Record.Id);

    public Task ClearAsync() => _history.ClearAsync();

    private PurchaseHistoryItem CreateItem(PurchaseRecord record, CultureInfo culture)
    {
        // Prefer the current localized city name; the stored one covers removed cities
        var city = DataSource.Cities.FirstOrDefault(c => c.Id == record.CityId);
        var cityName = city?.Name ?? record.CityName;
        var local = TimeZoneInfo.ConvertTime(record.PreparedAt, TimeZoneInfo.Local);
        var validity = string.IsNullOrEmpty(record.TimeText) ? _localizer.Duplicate : record.TimeText;
        var dateText = $"{local.ToString("M", culture)}, {local.ToString("t", culture)}";
        var priceText = FormatPrice(record.Price);
        var name = $"{cityName}, {validity}, {priceText}, {dateText}";

        return new(
            record,
            cityName,
            city?.Abbreviation ?? record.CityId,
            validity,
            priceText,
            dateText,
            name,
            string.Format(culture, _localizer.DeleteEntryFormat, name));
    }

    private string FormatPrice(int price) => $"{price} {_localizer["CzechPriceTag"]}";

    private static string MonthHeader(int year, int month, CultureInfo culture)
    {
        var text = new DateTime(year, month, 1).ToString("Y", culture);
        return text.Length > 0 ? char.ToUpper(text[0], culture) + text[1..] : text;
    }
}

public sealed record PurchaseHistoryMonth(string Header, string TotalText, IReadOnlyList<PurchaseHistoryItem> Items);

public sealed record PurchaseHistoryItem(
    PurchaseRecord Record,
    string CityName,
    string Abbreviation,
    string ValidityText,
    string PriceText,
    string DateText,
    string AutomationName,
    string DeleteLabel);
