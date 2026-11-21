using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using Windows.UI.StartScreen;

namespace SmsTicket.ViewModels;

public class MainViewModel : ObservableObject
{
    private const string DefaultCityId = "PHA";

    private readonly ISettingsService _settings;
    private readonly ISmsService _smsService;
    private readonly IAppSettings _appSettings;
    private readonly LocalizationService _localizer = new();

    private JumpList? _jumpList;
    private City _selectedCity;
    private TicketType? _selectedTicketType;

    public MainViewModel(ISettingsService settings, ISmsService smsService)
    {
        _settings = settings;
        _smsService = smsService;
        _appSettings = new AppSettings(_settings);

        Cities = DataSource.Cities;

        var lastCityId = _settings.GetSetting("LastCitySelected", () => DefaultCityId, false);

        // A city may have been removed since the last launch
        _selectedCity = Cities.FirstOrDefault(c => c.Id == lastCityId)
            ?? Cities.FirstOrDefault(c => c.Id == DefaultCityId)
            ?? Cities[0];
        SelectDefaultTicket();

        PrepareForSendCommand = new AsyncRelayCommand(PrepareForSendAsync);
        TogglePinCommand = new AsyncRelayCommand(TogglePinAsync);
    }

    public IReadOnlyList<City> Cities { get; }

    public IAsyncRelayCommand PrepareForSendCommand { get; }

    public IAsyncRelayCommand TogglePinCommand { get; }

    public XamlRoot? XamlRoot { get; set; }

    public bool IsPinningSupported { get; private set; }

    public City SelectedCity
    {
        get => _selectedCity;
        set
        {
            // ListView clears its selection when the flyout is re-templated
            if (value is null || !SetProperty(ref _selectedCity, value))
            {
                return;
            }

            _settings.SetSetting("LastCitySelected", value.Id, false);
            SelectDefaultTicket();
        }
    }

    public TicketType? SelectedTicketType
    {
        get => _selectedTicketType;
        set
        {
            if (value is null || !SetProperty(ref _selectedTicketType, value))
            {
                return;
            }

            _settings.SetSetting("CityTicket" + SelectedCity.Id, value.Id, false);
            OnPropertyChanged(nameof(IsPinned));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    public string SendButtonText => SelectedTicketType is { } ticket
        ? $"{_localizer.PrepareToSend} · {ticket.Price} {_localizer["CzechPriceTag"]}"
        : _localizer.PrepareToSend;

    public bool IsPinned =>
        _jumpList is not null &&
        SelectedTicketType is not null &&
        _jumpList.Items.Any(item => item.Arguments == SelectedTicketType.Id);

    public async Task InitAsync()
    {
        if (_jumpList is not null || !JumpList.IsSupported())
        {
            return;
        }

        _jumpList = await JumpList.LoadCurrentAsync();
        _jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
        IsPinningSupported = true;
        OnPropertyChanged(nameof(IsPinningSupported));
        OnPropertyChanged(nameof(IsPinned));
    }

    private void SelectDefaultTicket()
    {
        var savedTicketId = _settings.GetSetting<string?>("CityTicket" + _selectedCity.Id, () => null, false);
        var ticket = _selectedCity.TicketTypes.FirstOrDefault(t => t.Id == (savedTicketId ?? _selectedCity.DefaultTicketId))
            ?? _selectedCity.TicketTypes.FirstOrDefault();

        _selectedTicketType = null;
        SelectedTicketType = ticket;
    }

    private async Task PrepareForSendAsync()
    {
        if (SelectedTicketType is null)
        {
            return;
        }

        if (_appSettings.FirstTimeUse)
        {
            ContentDialog dialog = new()
            {
                Title = _localizer.PrepareSmsNoteTitle,
                Content = _localizer.PrepareSmsNote,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
            _appSettings.FirstTimeUse = false;
        }

        await _smsService.PrepareSmsAsync(SelectedTicketType.PhoneNumber, SelectedTicketType.SmsText);
    }

    private async Task TogglePinAsync()
    {
        if (_jumpList is null || SelectedTicketType is not { } ticket)
        {
            return;
        }

        if (!IsPinned)
        {
            var isDuplicate = ticket.IsDuplicate;
            var item = JumpListItem.CreateWithArguments(
                ticket.Id,
                $"{SelectedCity.Name} - {(isDuplicate ? _localizer.Duplicate : ticket.TimeText)}");
            item.Logo = new Uri(isDuplicate ? "ms-appx:///Assets/ticket_dupe.png" : "ms-appx:///Assets/ticket.png");
            item.Description = string.Format(_localizer.QuickActionDescriptionFormatString, ticket.Price);
            _jumpList.Items.Add(item);
        }
        else if (_jumpList.Items.FirstOrDefault(i => i.Arguments == ticket.Id) is { } existing)
        {
            _jumpList.Items.Remove(existing);
        }

        await _jumpList.SaveAsync();
        OnPropertyChanged(nameof(IsPinned));

        if (IsPinned)
        {
            ContentDialog dialog = new()
            {
                Title = _localizer.PinnedTitle,
                Content = _localizer.PinnedDescription,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
        }
    }
}
