using System.Collections.ObjectModel;
using SmsTicket.Data;
using SmsTicket.Data.Models;
using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Favorites;
using SmsTicket.Services.Localizer;
using SmsTicket.Services.Location;
using SmsTicket.Services.Settings;
using SmsTicket.Services.Sms;
using Windows.UI.StartScreen;

namespace SmsTicket.ViewModels;

public class MainViewModel : ObservableObject
{
    private const string DefaultCityId = "PHA";
    private const string FavoriteHintShownKey = "FavoriteHintShown";

    private readonly ISettingsService _settings;
    private readonly ISmsService _smsService;
    private readonly IAppSettings _appSettings;
    private readonly FavoritesStore _favorites;
    private readonly LocalizationService _localizer = new();

    private JumpList? _jumpList;
    private City _selectedCity;
    private TicketType? _selectedTicketType;

    public MainViewModel(ISettingsService settings, ISmsService smsService, ILocationProvider? locationProvider = null)
    {
        _settings = settings;
        _smsService = smsService;
        _appSettings = new AppSettings(_settings);
        _favorites = new FavoritesStore(_settings, id => DataSource.FindTicket(id) is not null);
        _favorites.Changed += (_, _) => RefreshFavorites();

        Cities = DataSource.Cities;
        RefreshFavorites();

        var lastCityId = _settings.GetSetting("LastCitySelected", () => DefaultCityId, false);

        // A city may have been removed since the last launch
        _selectedCity = Cities.FirstOrDefault(c => c.Id == lastCityId)
            ?? Cities.FirstOrDefault(c => c.Id == DefaultCityId)
            ?? Cities[0];
        SelectDefaultTicket();

        PrepareForSendCommand = new AsyncRelayCommand(() => PrepareForSendAsync(SelectedTicketType));
        BuyFavoriteCommand = new AsyncRelayCommand<CityTicket>(favorite => PrepareForSendAsync(favorite?.Ticket));
        RemoveFavoriteCommand = new AsyncRelayCommand<CityTicket>(RemoveFavoriteAsync);
        ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavoriteAsync);

        NearestCity = new NearestCityViewModel(
            locationProvider ?? new GeolocatorLocationProvider(),
            _appSettings,
            Cities,
            () => SelectedCity,
            city => SelectedCity = city);
    }

    public NearestCityViewModel NearestCity { get; }

    public IReadOnlyList<City> Cities { get; }

    public ObservableCollection<CityTicket> Favorites { get; } = new();

    public IAsyncRelayCommand PrepareForSendCommand { get; }

    public IAsyncRelayCommand<CityTicket> BuyFavoriteCommand { get; }

    public IAsyncRelayCommand<CityTicket> RemoveFavoriteCommand { get; }

    public IAsyncRelayCommand ToggleFavoriteCommand { get; }

    public XamlRoot? XamlRoot { get; set; }

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
            OnPropertyChanged(nameof(IsFavorite));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    public string SendButtonText => SelectedTicketType is { } ticket
        ? $"{_localizer.PrepareToSend} · {ticket.Price} {_localizer["CzechPriceTag"]}"
        : _localizer.PrepareToSend;

    public bool IsFavorite => SelectedTicketType is { } ticket && _favorites.Contains(ticket.Id);

    public bool HasFavorites => Favorites.Count > 0;

    public async Task InitAsync()
    {
        if (_jumpList is not null || !JumpList.IsSupported())
        {
            return;
        }

        _jumpList = await JumpList.LoadCurrentAsync();
        _jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

        // Tickets pinned before favorites existed live only in the JumpList
        if (!_settings.GetSetting(FavoritesMigration.MigratedSettingKey, () => false))
        {
            _favorites.ReplaceAll(FavoritesMigration.Merge(_favorites.Ids, _jumpList.Items.Select(i => i.Arguments)));
            _settings.SetSetting(FavoritesMigration.MigratedSettingKey, true);
        }

        await SyncJumpListAsync();
    }

    private void RefreshFavorites()
    {
        Favorites.Clear();
        foreach (var id in _favorites.Ids)
        {
            if (DataSource.FindTicket(id) is { } favorite)
            {
                Favorites.Add(favorite);
            }
        }

        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(IsFavorite));
    }

    private void SelectDefaultTicket()
    {
        var savedTicketId = _settings.GetSetting<string?>("CityTicket" + _selectedCity.Id, () => null, false);
        var ticket = _selectedCity.TicketTypes.FirstOrDefault(t => t.Id == (savedTicketId ?? _selectedCity.DefaultTicketId))
            ?? _selectedCity.TicketTypes.FirstOrDefault();

        _selectedTicketType = null;
        SelectedTicketType = ticket;
    }

    private async Task PrepareForSendAsync(TicketType? ticket)
    {
        if (ticket is null)
        {
            return;
        }

        if (_appSettings.FirstTimeUse)
        {
            await ShowInfoAsync(_localizer.PrepareSmsNoteTitle, _localizer.PrepareSmsNote);
            _appSettings.FirstTimeUse = false;
        }

        await _smsService.PrepareSmsAsync(ticket.PhoneNumber, ticket.SmsText);
    }

    private async Task ToggleFavoriteAsync()
    {
        if (SelectedTicketType is not { } ticket)
        {
            return;
        }

        var isFavorite = _favorites.Toggle(ticket.Id);
        await SyncJumpListAsync();

        if (isFavorite && _jumpList is not null && !_settings.GetSetting(FavoriteHintShownKey, () => false))
        {
            _settings.SetSetting(FavoriteHintShownKey, true);
            await ShowInfoAsync(_localizer.FavoriteAddedTitle, _localizer.FavoriteAddedDescription);
        }
    }

    private async Task RemoveFavoriteAsync(CityTicket? favorite)
    {
        if (favorite is not null && _favorites.Remove(favorite.Ticket.Id))
        {
            await SyncJumpListAsync();
        }
    }

    private async Task SyncJumpListAsync()
    {
        if (_jumpList is null)
        {
            return;
        }

        JumpListTexts texts = new(_localizer.Duplicate, _localizer.QuickActionDescriptionFormatString);
        _jumpList.Items.Clear();
        foreach (var spec in FavoriteJumpListMapper.ToSpecs(Favorites, texts))
        {
            var item = JumpListItem.CreateWithArguments(spec.Arguments, spec.DisplayName);
            item.Description = spec.Description;
            item.Logo = new Uri(spec.Logo);
            _jumpList.Items.Add(item);
        }

        try
        {
            await _jumpList.SaveAsync();
        }
        catch (Exception ex)
        {
            // Home screen shortcuts are a bonus, favorites still work in-app
            System.Diagnostics.Debug.WriteLine($"Failed to sync favorites to the JumpList: {ex}");
        }
    }

    private async Task ShowInfoAsync(string title, string content)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
